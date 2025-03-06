using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Helper;
using ChatClient.DataBase.Data;
using ChatClient.DataBase.UnitOfWork;
using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.BaseService.Services;

public interface IUserService
{
    public Task<Bitmap> GetHeadImage(UserDto User);

    public Task<Dictionary<int, Bitmap>> GetHeadImages(UserDto User);

    public Task<UserDto?> GetUserDto(string id);

    public Task SaveUser(UserDto User);
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
            Bitmap bitmap = new Bitmap(Path.Combine(Environment.CurrentDirectory, "Assets", "DefaultHead.ico"));
            return bitmap;
        }

        // 获取头像
        byte[]? file =
            await _fileOperateHelper.GetFileForUser(Path.Combine(User.Id, "HeadImage"), $"head_{User.HeadIndex}.png");
        //byte[]? file = await _webApiHelper.GetCompressedFileAsync(Path.Combine("Users", User.Id, "HeadImage"),$"head_{User.HeadIndex}.png");
        if (file != null)
        {
            Bitmap bitmap;
            using var stream = new MemoryStream(file);
            // 从流加载Bitmap
            bitmap = new Bitmap(stream);

            return bitmap;
        }
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
            byte[]? file = await _fileOperateHelper.GetFileForUser(Path.Combine(User.Id, "HeadImage"), $"head_{i}.png");
            if (file == null) continue;
            Bitmap bitmap;
            using (var stream = new MemoryStream(file))
            {
                bitmap = new Bitmap(stream);
            }

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
        var response = await _messageHelper.SendMessageWithResponse<UserMessage>(request);
        if (response == null) return null;

        var user = _mapper.Map<UserDto>(response);

        var data_user = _mapper.Map<User>(user);

        // 本地数据库保存
        var respository = _unitOfWork.GetRepository<User>();
        var currentUser =
            await respository.GetFirstOrDefaultAsync(predicate: d => d.Id.Equals(user.Id), disableTracking: false);
        if (currentUser != null)
            currentUser.Copy(data_user);
        else
            await respository.InsertAsync(data_user);
        await _unitOfWork.SaveChangesAsync();

        // 头像获取
        user.HeadImage = await GetHeadImage(user);

        return user;
    }

    /// <summary>
    /// 保存用户信息
    /// </summary>
    /// <param name="User"></param>
    public async Task SaveUser(UserDto User)
    {
        // 服务器保存
        UserMessage message = _mapper.Map<UserMessage>(User);
        if (message == null) return;

        UpdateUserData updateUserData = new UpdateUserData { User = message };
        await _messageHelper.SendMessage(updateUserData);

        // 本地数据库保存
        User user = _mapper.Map<User>(User);
        if (user == null) return;

        var _unitOfWork = _scopedProvider.Resolve<IUnitOfWork>();

        var userRepository = _unitOfWork.GetRepository<User>();
        var currentUser = await userRepository.GetFirstOrDefaultAsync(predicate: d => d.Id.Equals(user.Id));
        if (currentUser != null)
            currentUser.Copy(user);
        else
            userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }
}