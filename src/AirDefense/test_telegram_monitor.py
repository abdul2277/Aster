import json
import os
from pathlib import Path
import tempfile
import time
import unittest
from datetime import datetime, timezone
from unittest.mock import patch
import telegram_monitor as t
import air_defense as core

STAMP = datetime.now(timezone.utc).isoformat()
SOURCE = {'name': 'Public channel', 'format': 'telegram', 'url': '@channelname'}


def preview(text='Report at 24.7262, 46.6507', username='channelname', stamp=STAMP):
    return f'''<html><body><div class="tgme_widget_message_wrap"><div class="tgme_widget_message text_not_supported" data-post="{username}/123"><div class="tgme_widget_message_text js-message_text" dir="auto">{text}</div><div class="tgme_widget_message_footer"><a class="tgme_widget_message_date" href="https://t.me/{username}/123"><time datetime="{stamp}">12:00</time></a></div></div></div></body></html>'''.encode()


class TelegramTests(unittest.TestCase):
    def test_username_normalization(self):
        for name in ['channelname', '@ChannelName', 'https://t.me/channelname', 'https://t.me/s/channelname/']:
            self.assertEqual(t.channel_url(name), 'https://t.me/s/channelname')
        for name in ['https://evil.test/name', 'https://t.me/+invite', 'https://t.me/c/123/1', 'https://t.me/channelname/123', 'https://t.me/s/channelname?before=1', '../private', 'https://t.me:444/channelname']:
            with self.assertRaises(ValueError):
                t.channel_url(name)

    def test_coordinate_formats(self):
        for text in ['24.7262,46.6507', 'latitude:24.7262 longitude:46.6507', '24.7262 N, 46.6507 E', '٢٤٫٧٢٦٢، ٤٦٫٦٥٠٧']:
            points = t.locations(text, [])
            self.assertEqual(points[0][:2], (24.7262, 46.6507), text)
        self.assertEqual(t.locations('24.72 S, 46.65 W', [])[0][:2], (-24.72, -46.65))

    def test_bad_pairs_and_duplicates(self):
        self.assertEqual(t.locations('91.000, 46.6507; 24.7262, 181.123; 2026/10/13', []), [])
        self.assertEqual(len(t.locations('24.7262, 46.6507 and 24.7262, 46.6507', [])), 1)

    def test_places_and_precedence(self):
        places = [{'name': 'Example District', 'aliases': ['حي المثال'], 'latitude': 10, 'longitude': 20}]
        self.assertIn('approximate', t.locations('Report in حي المثال', places)[0][2])
        self.assertEqual(t.locations('Example DistrictExtra', places), [])
        self.assertEqual(t.locations('Example District; 24.7262,46.6507', places)[0][:2], (24.7262,46.6507))
        self.assertEqual(t.locations('Unknown neighborhood', places), [])

    def test_longest_place_match(self):
        places = [{'name': 'Example', 'latitude': 1, 'longitude': 2}, {'name': 'North Example', 'latitude': 3, 'longitude': 4}]
        self.assertEqual(t.locations('North Example', places)[0][:2], (3,4))
        self.assertEqual(len(t.locations('North Example', places)), 1)

    def test_nested_html_timestamp_and_link(self):
        rows, skipped = t.parse_preview(preview('Alert <b>24.7262</b>,<br/>46.6507 &amp; update'), SOURCE)
        self.assertEqual((len(rows),skipped),(1,0))
        self.assertEqual(rows[0]['published'], STAMP)
        self.assertEqual(rows[0]['url'], 'https://t.me/channelname/123')
        self.assertIn('& update', rows[0]['title'])
        self.assertEqual(rows[0]['severity'],'unverified channel report')

    def test_unavailable_and_no_location(self):
        with self.assertRaises(ValueError):
            t.parse_preview(b'<html>Please sign in</html>',SOURCE)
        rows, skipped = t.parse_preview(preview('Nothing geolocated'),SOURCE)
        self.assertEqual((rows,skipped),([],1))
        rows, skipped = t.parse_preview(preview(username='differentchannel'),SOURCE)
        self.assertEqual((rows,skipped),([],1))

    def test_stable_point_ids(self):
        first=t.parse_preview(preview('24.7262,46.6507; 25.1234,47.1234'),SOURCE)[0]
        second=t.parse_preview(preview('25.1234,47.1234; 24.7262,46.6507'),SOURCE)[0]
        self.assertEqual({r['id'] for r in first},{r['id'] for r in second})

    def test_pipeline_proximity_and_dedup(self):
        with patch.object(core,'request_bytes',return_value=preview()), patch.object(core,'load_places',return_value=[]):
            events, skipped=core.fetch(SOURCE)
        cfg={'home':(24.7262,46.6507),'radius_km':20,'max_age_seconds':900}
        self.assertTrue(core.eligible(events[0],cfg,time.time()))
        cfg['home']=(0,0)
        self.assertFalse(core.eligible(events[0],cfg,time.time()))
        cfg['home']=(24.7262,46.6507)
        messages=[]
        class Sender:
            def send(self,message): messages.append(message)
        store=core.Store(':memory:')
        for _ in range(2):
            store.enqueue(events[0],time.time()+600)
            core.drain(store,cfg,Sender(),time.time())
        self.assertEqual(len(messages),1)
        store.close()

    def test_gazetteer_validation(self):
        with tempfile.TemporaryDirectory(dir=os.environ.get('ASTER_TEST_TEMP')) as directory:
            path=Path(directory)/'places.json'
            self.assertEqual(t.load_places(path),[])
            path.write_text(json.dumps({'places':[{'name':'Same','latitude':0,'longitude':0},{'name':'same','latitude':1,'longitude':1}]}))
            with self.assertRaises(ValueError): t.load_places(path)

    def test_invalid_time_rejected_by_adapter(self):
        with patch.object(core,'request_bytes',return_value=preview(stamp='not a timestamp')), patch.object(core,'load_places',return_value=[]):
            events, skipped=core.fetch(SOURCE)
        self.assertEqual((events,skipped),([],1))

    def test_oversize_rejected(self):
        with self.assertRaises(ValueError): t.parse_preview(b'x'*(2*1024*1024+1),SOURCE)


if __name__=='__main__': unittest.main(verbosity=2)
