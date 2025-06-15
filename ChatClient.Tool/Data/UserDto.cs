using Avalonia.Media.Imaging;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.UIEntity;

namespace ChatClient.Tool.Data
{
    public class UserDto : BindableBase, IDisposable
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

        public DateOnly? Birth
        {
            get => birth;
            set => SetProperty(ref birth, value);
        }

        private DateOnly? birth;

        public string? Introduction
        {
            get => introduction;
            set => SetProperty(ref introduction, value);
        }

        private string? introduction;

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

        public bool IsUser
        {
            get => isUser;
            set => SetProperty(ref isUser, value);
        }

        private bool isUser = false;

        public bool IsFriend
        {
            get => isFriend;
            set => SetProperty(ref isFriend, value);
        }

        private bool isFriend = false;

        private string? remark;

        public string? Remark
        {
            get => remark;
            set => SetProperty(ref remark, value);
        }

        public DateTime RegisteTime
        {
            get => registeTime;
            set => SetProperty(ref registeTime, value);
        }

        private DateTime registeTime;

        public Bitmap HeadImage
        {
            get => headImage;
            set
            {
                if (SetProperty(ref headImage, value))
                {
                }
            }
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

        public event Action OnUserOnlineChanged;

        public void CopyFrom(UserDto dto)
        {
            Name = dto.Name;
            Introduction = dto.Introduction;
            HeadIndex = dto.HeadIndex;
            HeadCount = dto.HeadCount;
            HeadImage = dto.HeadImage;
            Birth = dto.birth;
            Sex = dto.Sex;
        }

        public void Dispose()
        {
            HeadImage = null;
            OnUserOnlineChanged = null;
        }
    }
}