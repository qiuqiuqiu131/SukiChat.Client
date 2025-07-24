using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.Media.EndPoint;

namespace ChatClient.Media.Android.EndPoint;

public class CameraEndPointFactory:IFactory<ICameraEndPoint>
{
    public ICameraEndPoint Create()
    {
        return new AndroidCameraEndPoint();
    }
}