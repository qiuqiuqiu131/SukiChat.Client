using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

/// <summary>
/// 用户信息服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取用户当前使用的头像
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="headIndex"></param>
    /// <returns></returns>
    public Task<Bitmap?> GetHeadImage(string userId, int headIndex);

    /// <summary>
    /// 获取用户的所有历史头像
    /// </summary>
    /// <param name="User"></param>
    /// <returns></returns>
    public Task<Dictionary<int, Bitmap>> GetHeadImages(UserDto User);

    /// <summary>
    /// 获取用户信息，作为用户基础实体，用于组装其他关系实体
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isUpdate"></param>
    /// <returns></returns>
    public Task<UserDto?> GetUserDto(string id, bool isUpdate = false);

    /// <summary>
    /// 根据UserMessage获取用户信息
    /// </summary>
    /// <param name="userMessage"></param>
    /// <returns></returns>
    public Task<UserDto> GetUserDto(UserMessage userMessage);

    /// <summary>
    /// 获取用户详细信息(当前登录者)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public Task<UserDetailDto?> GetUserDetailDto(string id, string password);

    /// <summary>
    /// 保存用户详细信息(当前登陆者)
    /// </summary>
    /// <param name="User"></param>
    /// <returns></returns>
    public Task<bool> SaveUser(UserDetailDto User);
}

internal class UserService : BaseService, IUserService
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
        try
        {
            if (headIndex == -1)
            {
                Bitmap bitmap = new Bitmap(Path.Combine(Environment.CurrentDirectory, "Assets", "DefaultHead.png"));
                return bitmap;
            }

            var imageManager = _scopedProvider.Resolve<IImageManager>();

            // 获取头像
            var file =
                await imageManager.GetFile(userId, "HeadImage", $"head_{headIndex}.png", FileTarget.User);
            if (file != null)
                return file;
            else
            {
                var path = Path.Combine(Environment.CurrentDirectory, "Assets", "DefaultHead.png");
                Bitmap? bitmap = null;
                if (System.IO.File.Exists(path))
                    bitmap = new Bitmap(path);
                return bitmap;
            }
        }
        catch (Exception e)
        {
            return new Bitmap(Path.Combine(Environment.CurrentDirectory, "Assets", "DefaultHead.png"));
        }
    }

    public async Task<Dictionary<int, Bitmap>> GetHeadImages(UserDto User)
    {
        Dictionary<int, Bitmap> bitmaps = new();
        foreach (var i in Enumerable.Range(0, User.HeadCount))
        {
            byte[]? file = await _fileOperateHelper.GetFile(User.Id, "HeadImage", $"head_{i}.png", FileTarget.User);
            if (file == null) continue;
            Bitmap bitmap;
            using (var stream = new MemoryStream(file))
                bitmap = new Bitmap(stream);

            Array.Clear(file);

            bitmaps.Add(i, bitmap);
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

        // try
        // {
        //     // 本地数据库保存
        //     var respository = _unitOfWork.GetRepository<User>();
        //     var currentUser =
        //         await respository.GetFirstOrDefaultAsync(predicate: d => d.Id.Equals(user.Id), disableTracking: false);
        //     if (currentUser != null)
        //     {
        //         currentUser.HeadIndex = userMessage.HeadIndex;
        //         currentUser.HeadCount = (int)userMessage.HeadCount;
        //         currentUser.Introduction = userMessage.Introduction;
        //         currentUser.Birthday =
        //             string.IsNullOrEmpty(userMessage.Birth) ? null : DateOnly.Parse(userMessage.Birth);
        //         currentUser.isMale = userMessage.IsMale;
        //         currentUser.Name = userMessage.Name;
        //     }
        //     else
        //     {
        //         var userEntity = new User
        //         {
        //             HeadCount = (int)userMessage.HeadCount,
        //             HeadIndex = userMessage.HeadIndex,
        //             Id = userMessage.Id,
        //             Name = userMessage.Name,
        //             Introduction = userMessage.Introduction,
        //             Birthday = string.IsNullOrEmpty(userMessage.Birth) ? null : DateOnly.Parse(userMessage.Birth),
        //             isMale = userMessage.IsMale
        //         };
        //         await respository.InsertAsync(userEntity);
        //     }
        //
        //     await _unitOfWork.SaveChangesAsync();
        // }
        // catch (Exception e)
        // {
        //     // ignore
        // }

        if (!isUpdated)
            _ = Task.Run(async () => user.HeadImage = await GetHeadImage(user));
        else
            user.HeadImage = await GetHeadImage(user, isUpdated);

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