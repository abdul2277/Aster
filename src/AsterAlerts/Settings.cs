using System.Collections.Generic;

namespace AsterAlerts;

public class Settings
{
	public string home_lat_env = "ASTER_HOME_LAT";

	public string home_lon_env = "ASTER_HOME_LON";

	public string telegram_token_env = "ASTER_TELEGRAM_TOKEN";

	public string telegram_chat_env = "ASTER_TELEGRAM_CHAT_ID";

	public string state_file = "air_defense.sqlite3";

	public double radius_km = 20.0;

	public double poll_seconds = 30.0;

	public double max_age_seconds = 900.0;

	public List<Feed> sources = new List<Feed>();
}
