using System.Collections.Generic;

namespace Aster;

internal class UpdateManifest
{
	public int Schema;

	public string AppId;

	public string Version;

	public List<UpdateFile> Files;
}
