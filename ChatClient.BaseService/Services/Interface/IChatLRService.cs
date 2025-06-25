using ChatClient.Tool.Data.Friend;
using ChatClient.Tool.Data.Group;

namespace ChatClient.BaseService.Services.Interface;

/// <summary>
/// 聊天消息加载和清理服务接口
/// </summary>
public interface IChatLRService
{
    /// <summary>
    /// 加载好友聊天记录,为FriendChatDto中添加新的聊天记录
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="friendChatDto"></param>
    /// <returns></returns>
    Task LoadFriendChatDto(string userId, FriendChatDto friendChatDto);

    /// <summary>
    /// 加载群组聊天记录，为GroupChatDto中添加新的聊天记录
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="groupChatDto"></param>
    /// <returns></returns>
    Task LoadGroupChatDto(string userId, GroupChatDto groupChatDto);

    /// <summary>
    /// 清理上选中对象的聊天记录
    /// </summary>
    /// <param name="friendChatDto"></param>
    void ClearFriendChatDto(FriendChatDto friendChatDto);

    /// <summary>
    /// 清理上选中对象的聊天记录
    /// </summary>
    /// <param name="groupChatDto"></param>
    void ClearGroupChatDto(GroupChatDto groupChatDto);
}