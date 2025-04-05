using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

public interface IUserService
{
    public Task<Bitmap> GetHeadImage(string userId, int headIndex);

    public Task<Dictionary<int, Bitmap>> GetHeadImages(UserDto User);

    public Task<UserDto?> GetUserDto(string id);

    public Task<Bitmap> GetHeadImage(UserDto User);

    public Task<UserDetailDto?> GetUserDetailDto(string id, string password);

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

    /// <summary>
    /// 获取用户头像
    /// </summary>
    /// <returns></returns>
    public async Task<Bitmap> GetHeadImage(UserDto User)
    {
        if (User.HeadCount == 0)
        {
            Bitmap bitmap = new Bitmap(Path.Combine(Environment.CurrentDirectory, "Assets", "DefaultHead.png"));
            return bitmap;
        }

        var imageManager = _scopedProvider.Resolve<IImageManager>();
        // 获取头像
        Bitmap? file =
            await imageManager.GetFile(User.Id, "HeadImage", $"head_{User.HeadIndex}.png", FileTarget.User);
        if (file != null)
            return file;
        else
        {
            Bitmap bitmap = new Bitmap(Path.Combine(Environment.CurrentDirectory, "Assets", "DefaultHead.png"));
            return bitmap;
        }
    }

    public async Task<Bitmap> GetHeadImage(string userId, int headIndex)
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
            Bitmap bitmap = new Bitmap(Path.Combine(Environment.CurrentDirectory, "Assets", "DefaultHead.ico"));
            return bitmap;
        }
    }

    /// <summary>
    /// 获取用户所有头像
    /// </summary>
    /// <param name="User"></param>
    /// <returns></returns>
    public async Task<Dictionary<int, Bitmap>> GetHeadImages(UserDto User)
    {
        Dictionary<int, Bitmap> bitmaps = new();
        foreach (var i in Enumerable.Range(0, User.HeadCount))
        {
            byte[]? file = await _fileOperateHelper.GetFile(User.Id, "HeadImage", $"head_{i}.png", FileTarget.User);
            if (file == null) continue;
            Bitmap bitmap;
            using (var stream = new MemoryStream(file))
            {
                bitmap = new Bitmap(stream);
            }

            Array.Clear(file);

            bitmaps.Add(i, bitmap);
        }

        return bitmaps;
    }

    /// <summary>
    /// 获取用户具体信息
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<UserDto?> GetUserDto(string id)
    {
        GetUserRequest request = new GetUserRequest() { Id = id };
        var response = await _messageHelper.SendMessageWithResponse<GetUserResponse>(request);
        if (response is not { Response: { State: true } }) return null;

        var userMessage = response.User;
        var user = _mapper.Map<UserDto>(userMessage);

        try
        {
            // 本地数据库保存
            var respository = _unitOfWork.GetRepository<User>();
            var currentUser =
                await respository.GetFirstOrDefaultAsync(predicate: d => d.Id.Equals(user.Id), disableTracking: false);
            if (currentUser != null)
            {
                currentUser.HeadIndex = (int)userMessage.HeadCount;
                currentUser.HeadCount = (int)userMessage.HeadCount;
                currentUser.Introduction = userMessage.Introduction;
                currentUser.Birthday =
                    string.IsNullOrEmpty(userMessage.Birth) ? null : DateOnly.Parse(userMessage.Birth);
                currentUser.isMale = userMessage.IsMale;
                currentUser.Name = userMessage.Name;
            }
            else
            {
                var userEntity = new User
                {
                    HeadCount = (int)userMessage.HeadCount,
                    HeadIndex = (int)userMessage.HeadCount,
                    Id = userMessage.Id,
                    Name = userMessage.Name,
                    Introduction = userMessage.Introduction,
                    Birthday = string.IsNullOrEmpty(userMessage.Birth) ? null : DateOnly.Parse(userMessage.Birth),
                    isMale = userMessage.IsMale
                };
                await respository.InsertAsync(userEntity);
            }

            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception e)
        {
            // ignore
        }

        Task.Run(async () => user.HeadImage = await GetHeadImage(user));

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

    /// <summary>
    /// 保存用户信息
    /// </summary>
    /// <param name="User"></param>
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