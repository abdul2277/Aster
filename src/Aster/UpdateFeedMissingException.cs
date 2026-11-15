using System.IO;

namespace Aster;

internal class UpdateFeedMissingException : IOException
{
	public UpdateFeedMissingException()
		: base("The update release or asset has not been published at the configured GitHub address.")
	{
	}
}
