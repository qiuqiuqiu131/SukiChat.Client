using Avalonia.Media.Imaging;
using ChatClient.Desktop.UIEntity;

namespace ChatClient.Tool.Data
{
    public class UserDto : BindableBase
    {
        public string Id
        {
            get => id;
            set => SetProperty(ref id, value);
        }

        private string id;

        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private string name;

        public Sex Sex
        {
            get => sex;
            set => SetProperty(ref sex, value);
        }

        private Sex sex;

        public DateOnly Birth
        {
            get => birth;
            set => SetProperty(ref birth, value);
        }

        private DateOnly birth;

        public string? Password
        {
            get => password;
            set => SetProperty(ref password, value);
        }

        private string? password;

        public string Introduction
        {
            get => introduction;
            set => SetProperty(ref introduction, value);
        }

        private string introduction;

        public int HeadIndex
        {
            get => headIndex;
            set => SetProperty(ref headIndex, value);
        }

        private int headIndex;

        public int HeadCount
        {
            get => headCount;
            set => SetProperty(ref headCount, value);
        }

        private int headCount;

        public DateTime RegisteTime
        {
            get => registeTime;
            set => SetProperty(ref registeTime, value);
        }

        private DateTime registeTime;

        public Bitmap HeadImage
        {
            get => headImage;
            set => SetProperty(ref headImage, value);
        }

        private Bitmap headImage;

        private bool _isOnline;

        public bool IsOnline
        {
            get => _isOnline;
            set
            {
                if (SetProperty(ref _isOnline, value))
                    OnUserOnlineChanged?.Invoke();
            }
        }

        private bool _cantDisturb;

        public bool CantDisturb
        {
            get => _cantDisturb;
            set => SetProperty(ref _cantDisturb, value);
        }

        public event Action OnUserOnlineChanged;
    }
}