using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Services.Interface;
using ChatClient.DataBase.EfCore.Data;
using ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.EfCore;

internal class UserService : Services.BaseService, IUserService
{
    private readonly IFileOperateHelper _fileOperateHelper;
    private readonly IMessageHelper _messageHelper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IFileOperateHelper fileOperateHelper,
        IMessageHelper messageHelper,
        IContainerProvider containerProvider,
        IMapper mapper) : base(containerProvider)
    {
        _fileOperateHelper = fileOperateHelper;
        _messageHelper = messageHelper;
        _mapper = mapper;
        _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
    }

    public async Task<Bitmap> GetHeadImage(UserDto User, bool isUpdate = false)
    {
        var imageManager = _scopedProvider.Resolve<IImageManager>();
        if (User.HeadCount == 0)
        {
            Bitmap bitmap = await imageManager.GetStaticFile(Path.Combine(Environment.CurrentDirectory, "Assets",
                "DefaultHead.png"));
            return bitmap;
        }

        if (isUpdate)
            imageManager.RemoveFromCache(User.Id, "HeadImage", $"head_{User.HeadIndex}.png", FileTarget.User);

        // 获取头像
        Bitmap? file =
            await imageManager.GetFile(User.Id, "HeadImage", $"head_{User.HeadIndex}.png", FileTarget.User);
        if (file != null)
            return file;
        else
        {
            Bitmap bitmap = await imageManager.GetStaticFile(Path.Combine(Environment.CurrentDirectory, "Assets",
                "DefaultHead.png"));
            return bitmap;
        }
    }

    public async Task<Bitmap?> GetHeadImage(string userId, int headIndex)
    {
        var imageManager = _scopedProvider.Resolve<IImageManager>();

        try
        {
            if (headIndex == -1)
            {
                Bitmap bitmap = await imageManager.GetStaticFile(Path.Combine(Environment.CurrentDirectory, "Assets",
                    "DefaultHead.png"));
                return bitmap;
            }

            // 获取头像
            var file =
                await imageManager.GetFile(userId, "HeadImage", $"head_{headIndex}.png", FileTarget.User);
            if (file != null)
                return file;
            else
            {
                return await imageManager.GetStaticFile(Path.Combine(Environment.CurrentDirectory, "Assets",
                    "DefaultHead.png"));
            }
        }
        catch (Exception e)
        {
            return await imageManager.GetStaticFile(Path.Combine(Environment.CurrentDirectory, "Assets",
                "DefaultHead.png"));
        }
    }

    public async Task<Dictionary<int, Bitmap>> GetHeadImages(UserDto User)
    {
        Dictionary<int, Bitmap> bitmaps = new();
        foreach (var i in Enumerable.Range(0, User.HeadCount))
        {
            try
            {
                await using var stream =
                    await _fileOperateHelper.GetFile(User.Id, "HeadImage", $"head_{i}.png", FileTarget.User);
                var bitmap = new Bitmap(stream);
                bitmaps.Add(i, bitmap);
            }
            catch (NullReferenceException e)
            {
            }
        }

        return bitmaps;
    }

    public async Task<UserDto?> GetUserDto(string id, bool isUpdated)
    {
        // 远程获取用户信息
        GetUserRequest request = new GetUserRequest() { Id = id };
        var response = await _messageHelper.SendMessageWithResponse<GetUserResponse>(request);
        if (response is not { Response: { State: true } }) return null;

        var userMessage = response.User;
        var user = _mapper.Map<UserDto>(userMessage);

        if (!isUpdated)
            _ = Task.Run(async () => user.HeadImage = await GetHeadImage(user));
        else
            user.HeadImage = await GetHeadImage(user, isUpdated);

        try
        {
            var _user = _mapper.Map<User>(user);
            var userRepository = _unitOfWork.GetRepository<User>();
            userRepository.Update(_user);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
        }


        return user;
    }

    public async Task<UserDto> GetUserDto(UserMessage userMessage)
    {
        var user = _mapper.Map<UserDto>(userMessage);
        _ = Task.Run(async () => user.HeadImage = await GetHeadImage(user));

        return user;
    }

    public async Task<UserDetailDto?> GetUserDetailDto(string id, string password)
    {
        var message = new GetUserDetailMessageRequest
        {
            Id = id,
            Password = password
        };

        var result = await _messageHelper.SendMessageWithResponse<GetUserDetailMessageResponse>(message);
        if (result is { Response: { State: true } })
        {
            var userDetail = _mapper.Map<UserDetailDto>(result.User);
            return userDetail;
        }

        return null;
    }

    public async Task<bool> SaveUser(UserDetailDto User)
    {
        // 服务器保存
        UserDetailMessage message = _mapper.Map<UserDetailMessage>(User);
        if (message == null) return false;

        UpdateUserDataRequest updateUserData = new UpdateUserDataRequest { User = message, UserId = User.Id };
        var result = await _messageHelper.SendMessageWithResponse<UpdateUserDataResponse>(updateUserData);

        if (result is { Response: { State: true } })
        {
            // 本地数据库保存
            User user = _mapper.Map<User>(User);
            if (user == null) return false;

            try
            {
                var _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();
                var userRepository = _unitOfWork.GetRepository<User>();
                userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // doNothing
            }

            return true;
        }

        return false;
    }
}