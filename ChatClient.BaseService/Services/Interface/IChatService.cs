using ChatClient.Tool.Data.ChatMessage;
using ChatClient.Tool.HelperInterface;

namespace ChatClient.BaseService.Services.Interface;

public interface IChatService
{
    /// <summary>
    /// 发送私聊消息，如果发送图片消息，会上传图片，再发送消息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetId">好友ID</param>
    /// <param name="messages">消息内容</param>
    /// <returns></returns>
    Task<(bool, string)> SendChatMessage(string userId, string targetId, List<ChatMessageDto> messages);

    /// <summary>
    /// 发送群聊消息，如果发送图片消息，会上传图片，再发送消息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="groupId">群聊ID</param>
    /// <param name="messages">消息内容</param>
    /// <returns></returns>
    Task<(bool, string)> SendGroupChatMessage(string userId, string groupId, List<ChatMessageDto> messages);

    /// <summary>
    /// 处理聊天消息，用于注入消息资源，如：图片、语音、文件等
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="id">群聊ID\好友ID</param>
    /// <param name="chatId">消息ID</param>
    /// <param name="isUser">是否为用户发送消息</param>
    /// <param name="chatMessages">消息体内容</param>
    /// <param name="fileTarget">消息发送目标（个人\群聊）</param>
    /// <returns></returns>
    Task OperateChatMessage(string userId, string id, int chatId, bool isUser, List<ChatMessageDto> chatMessages,
        FileTarget fileTarget);

    /// <summary>
    /// 向好友发送输入状态的消息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetId">好友ID</param>
    /// <param name="isWriting">输入状态</param>
    /// <returns></returns>
    Task SendFriendWritingMessage(string? userId, string? targetId, bool isWriting);

    /// <summary>
    /// 用于更新文件下载状态,数据库中更改，如未下载、已下载并保存在本地
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="fileMess">文件消息</param>
    /// <param name="target">消息发送目标（个人\群聊）</param>
    /// <returns></returns>
    Task UpdateFileMess(string userId, FileMessDto fileMess, FileTarget target);

    /// <summary>
    /// 读取所有聊天消息，将所有未读消息设置为已读
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="targetId">好友ID</param>
    /// <param name="chatId">最新的聊天消息ID</param>
    /// <param name="fileTarget">消息发送目标（个人\群聊）</param>
    /// <returns></returns>
    Task<bool> ReadAllChatMessage(string userId, string targetId, int chatId, FileTarget fileTarget);

    /// <summary>
    /// 确保聊天对象在聊天列表中，如果不存在，则添加
    /// </summary>
    /// <param name="obj">GroupRelationDto\FriendRelationDto</param>
    /// <returns></returns>
    Task AddChatDto(object obj);

    /// <summary>
    /// 删除某条聊天记录，用户将不会在看到这条消息，当对方仍然能够看到
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="chatId">聊天消息ID</param>
    /// <param name="fileTarget">群聊\好友</param>
    /// <returns></returns>
    Task<bool> DeleteChatMessage(string userId, int chatId, FileTarget fileTarget);

    /// <summary>
    /// 撤回聊天消息，用户和对方都看不到这条消息，但能看到撤回的提示
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="chatId">聊天消息ID</param>
    /// <param name="fileTarget">群聊\好友</param>
    /// <returns></returns>
    Task<bool> RetractChatMessage(string userId, int chatId, FileTarget fileTarget);

    /// <summary>
    /// 分享聊天消息到其他人或者群聊
    /// </summary>
    /// <param name="userId">当前用户ID</param>
    /// <param name="chatMessageDto">分享目标消息</param>
    /// <param name="senderMessage">分享备注</param>
    /// <param name="relations">分享目标(好友、群聊)</param>
    /// <returns></returns>
    Task<bool> SendChatShareMessage(string userId, ChatMessageDto chatMessageDto, string senderMessage,
        IEnumerable<object> relations);

    /// <summary>
    /// 获取文件聊天信息的文件下载状态
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="chatId"></param>
    /// <param name="fileTarget"></param>
    /// <returns></returns>
    Task<string?> GetTargetPath(string userId, int chatId, FileTarget fileTarget);

    Task<string?> OperateRetractedChatMessage(string userId, int chatId, FileTarget fileTarget);
}