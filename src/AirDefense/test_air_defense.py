import io
import json
import math
import os
from pathlib import Path
import tempfile
import time
import unittest
from unittest.mock import patch
from datetime import datetime, timezone
from urllib.error import HTTPError
import air_defense as a

NOW = time.time()
STAMP = datetime.fromtimestamp(NOW, timezone.utc).isoformat()
CFG = {'home': (0, 0), 'radius_km': 20, 'max_age_seconds': 900, 'poll_seconds': 30,
       'sources': [{'name': 'Public', 'format': 'json', 'url': 'https://example.org/feed'}]}


def row(**changes):
    data = dict(id='notice-1', title='Public advisory', latitude=0, longitude=.1, published=STAMP)
    data.update(changes)
    return data


def event(**changes):
    return a.event_from(row(**changes), 'Public')


class Sender:
    def __init__(self, fail=False):
        self.fail, self.messages = fail, []

    def send(self, text):
        if self.fail:
            raise a.DeliveryError(45)
        self.messages.append(text)


class Tests(unittest.TestCase):
    def test_distance(self):
        self.assertEqual(a.haversine_km(0, 0, 0, 0), 0)
        self.assertAlmostEqual(a.haversine_km(0, 0, 0, 1), 111.19508, places=4)
        self.assertAlmostEqual(a.haversine_km(0, 179.9, 0, -179.9), 22.239, places=3)
        self.assertAlmostEqual(a.haversine_km(0, 0, 0, 180), math.pi * 6371.0088)

    def test_invalid_coordinates(self):
        for x in [91, float('nan'), float('inf'), None, True]:
            with self.assertRaises((ValueError, TypeError)):
                a.coordinates(x, 0)

    def test_radius_boundary_and_age(self):
        e = event()
        self.assertTrue(a.eligible(e, CFG, NOW))
        self.assertFalse(a.eligible(event(longitude=1), CFG, NOW))
        exact = dict(CFG, radius_km=a.haversine_km(0, 0, e.lat, e.lon))
        self.assertTrue(a.eligible(e, exact, NOW))
        self.assertFalse(a.eligible(e, CFG, NOW + 901))
        self.assertFalse(a.eligible(e, CFG, NOW - 121))
        expired = event(expires=datetime.fromtimestamp(NOW - 1, timezone.utc).isoformat())
        self.assertFalse(a.eligible(expired, CFG, NOW))

    def test_timestamps(self):
        self.assertEqual(a.timestamp('2026-10-13T12:00:00+03:00'), a.timestamp('2026-10-13T09:00:00Z'))
        self.assertEqual(a.timestamp('Tue, 13 Oct 2026 09:00:00 GMT'), a.timestamp('2026-10-13T09:00:00Z'))
        with self.assertRaises(ValueError):
            a.timestamp('2026-10-13T09:00:00')

    def test_json_bad_record_isolation(self):
        rows = [row(), row(latitude=999), row(published=None), row(id='')]
        events, skipped = a.parse_feed(json.dumps({'alerts': rows}).encode(), CFG['sources'][0])
        self.assertEqual((len(events), skipped), (1, 3))

    def test_geojson_axis_order(self):
        data = {'type': 'FeatureCollection', 'features': [
            {'type': 'Feature', 'id': 'geo-1', 'geometry': {'type': 'Point', 'coordinates': [46.7, 24.7]}, 'properties': row()},
            {'geometry': {'type': 'Polygon'}, 'properties': row()}]}
        result, skipped = a.parse_feed(json.dumps(data).encode(), {'name': 'Geo', 'format': 'geojson'})
        self.assertEqual((result[0].lat, result[0].lon, skipped), (24.7, 46.7, 1))

    def test_rss_and_atom(self):
        rss = f'<rss xmlns:georss="http://www.georss.org/georss"><channel><item><guid>1</guid><title>Advisory</title><pubDate>{STAMP}</pubDate><georss:point>24.7 46.7</georss:point></item></channel></rss>'
        results, skipped = a.parse_feed(rss.encode(), {'name': 'RSS', 'format': 'rss'})
        self.assertEqual((results[0].lat, results[0].lon, skipped), (24.7, 46.7, 0))
        atom = f'<feed xmlns="http://www.w3.org/2005/Atom" xmlns:g="http://www.w3.org/2003/01/geo/wgs84_pos#"><entry><id>1</id><title>A</title><updated>{STAMP}</updated><g:lat>0</g:lat><g:long>0.1</g:long></entry></feed>'
        results, skipped = a.parse_feed(atom.encode(), {'name': 'Atom', 'format': 'rss'})
        self.assertEqual((results[0].lon, skipped), (.1, 0))

    def test_xml_and_size_guards(self):
        for data in [b'<!DOCTYPE rss><rss/>', b'<!ENTITY a "x"><rss/>', b'x' * (a.MAX_BYTES + 1), b'<notrss/>']:
            with self.assertRaises(ValueError):
                a.parse_feed(data, {'name': 'RSS', 'format': 'rss'})

    def test_persistent_delivery_and_dedup(self):
        with tempfile.TemporaryDirectory(dir=os.environ.get("ASTER_TEST_TEMP")) as directory:
            path = Path(directory) / 'state.db'
            db = a.Store(path)
            e = event()
            db.enqueue(e, NOW + 600)
            db.enqueue(e, NOW + 600)
            db.close()
            db = a.Store(path)
            sender = Sender()
            a.drain(db, CFG, sender, NOW)
            a.drain(db, CFG, sender, NOW)
            self.assertEqual(len(sender.messages), 1)
            self.assertEqual(db.db.execute('SELECT sent FROM alerts').fetchone()[0], 1)
            db.close()

    def test_failed_delivery_retries(self):
        db = a.Store(':memory:')
        db.enqueue(event(), NOW + 600)
        sender = Sender(True)
        due = a.drain(db, CFG, sender, NOW)
        self.assertEqual(due, NOW + 45)
        self.assertEqual(db.db.execute('SELECT sent,attempts FROM alerts').fetchone(), (0, 1))
        sender.fail = False
        a.drain(db, CFG, sender, NOW + 44)
        self.assertEqual(len(sender.messages), 0)
        a.drain(db, CFG, sender, NOW + 45)
        self.assertEqual(len(sender.messages), 1)
        db.close()

    def test_expired_queue_not_delivered(self):
        db = a.Store(':memory:')
        db.enqueue(event(), NOW - 1)
        sender = Sender()
        a.drain(db, CFG, sender, NOW)
        self.assertEqual(sender.messages, [])
        db.close()

    def test_expiry_rechecked_before_send(self):
        db = a.Store(':memory:')
        db.enqueue(event(), NOW + 600)
        sender = Sender()
        a.drain(db, CFG, sender, NOW, clock=lambda: NOW + 901)
        self.assertEqual(sender.messages, [])
        db.close()

    def test_revision_key(self):
        self.assertEqual(event().key(), event(published='2026-10-13T12:00:00Z').key())
        self.assertNotEqual(event().key(), event(title='Revised advisory').key())

    def test_telegram_payload(self):
        captured = {}
        def transport(url, **kwargs):
            captured.update(json.loads(kwargs['body']))
            return b'{"ok":true,"result":{"message_id":1}}'
        a.Telegram('123:secret', '789', transport).send('Report')
        self.assertEqual(captured['chat_id'], '789')
        self.assertFalse(captured['disable_notification'])
        self.assertNotIn('parse_mode', captured)

    def test_telegram_failure_and_rate_limit(self):
        def rate_limit(*args, **kwargs):
            raise HTTPError('https://api.telegram.org/botSECRET', 429, 'Rate limit', {}, io.BytesIO(b'{"parameters":{"retry_after":77}}'))
        with self.assertRaises(a.DeliveryError) as caught:
            a.Telegram('123:secret', '789', rate_limit).send('Report')
        self.assertEqual(caught.exception.retry_after, 77)
        self.assertNotIn('SECRET', str(caught.exception))
        with self.assertRaises(a.DeliveryError):
            a.Telegram('123:secret', '789', lambda *args, **kwargs: b'{"ok":false}').send('Report')

    def test_source_failure_does_not_block_other_source(self):
        cfg = dict(CFG, sources=CFG['sources'] + [{'name': 'Broken'}])
        def fetch(source):
            if source['name'] == 'Broken':
                raise OSError('Secret URL must not be logged')
            return [event()], 0
        db, sender = a.Store(':memory:'), Sender()
        with patch.object(a, 'fetch', fetch), self.assertLogs(a.LOG) as logs:
            a.run(cfg, db, sender, once=True)
        self.assertEqual(len(sender.messages), 1)
        self.assertNotIn('Secret URL', ' '.join(logs.output))
        db.close()

    def test_dry_run_never_sends(self):
        db, sender = a.Store(':memory:'), Sender()
        with patch.object(a, 'fetch', return_value=([event()], 0)):
            a.run(CFG, db, sender, once=True, dry_run=True)
        self.assertEqual(sender.messages, [])
        db.close()

    def test_run_lock(self):
        with tempfile.TemporaryDirectory(dir=os.environ.get("ASTER_TEST_TEMP")) as directory:
            path = Path(directory) / 'state'
            lock = a.RunLock(path)
            try:
                with self.assertRaises((ValueError, OSError)):
                    a.RunLock(path)
            finally:
                lock.close()
            lock = a.RunLock(path)
            lock.close()

    def test_unsafe_urls(self):
        for value in ['http://example.org', 'https://name:password@example.org', 'file:///private']:
            with self.assertRaises(ValueError):
                a.https_url(value)


if __name__ == '__main__':
    unittest.main(verbosity=2)
