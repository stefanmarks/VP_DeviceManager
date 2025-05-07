using System.Collections.Generic;
using System.Text;

public interface IDevice
{
	public string GetDeviceName();
	public void   GetDeviceInformation(StringBuilder sb, string prefix);
}

public class IDeviceComparer : IComparer<IDevice>
{
	public int Compare(IDevice x, IDevice y)
	{
		return string.Compare(x.GetDeviceName(), y.GetDeviceName());
	}

	public static readonly IDeviceComparer INSTANCE = new IDeviceComparer();
}
