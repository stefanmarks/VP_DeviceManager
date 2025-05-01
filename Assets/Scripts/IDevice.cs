using System.Text;

public interface IDevice
{
	public string GetDeviceName();
	public void   GetDeviceInformation(StringBuilder sb);
}
