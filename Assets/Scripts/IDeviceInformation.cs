using System.Collections.Generic;

public interface IDeviceInformation 
{
	public string GetDeviceName();
	public void   GetData(List<string> data);
}
