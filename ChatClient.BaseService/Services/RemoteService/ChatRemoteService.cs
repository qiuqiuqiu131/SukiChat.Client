using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;
using System.Collections.Generic;

namespace ChatClient.BaseService.Services.RemoteService;

public interface IChatRemoteService
{
    /// <summary>
    /// 批量获取好友聊天记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginTime">上次登录时间</param>
    /// <returns></returns>
    Task<List<FriendChatMessage>> GetFriendChatMessages(string userId, DateTime loginTime);

    /// <summary>
    /// 批量获取群聊聊天记录
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginTime">上次登录时间</param>
    /// <returns></returns>
    Task<List<GroupChatMessage>> GetGroupChatMessages(string userId, DateTime loginTime);

    /// <summary>
    /// 批量获取好友聊天详情信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginTime">上次登录时间</param>
    /// <returns></returns>
    Task<List<ChatPrivateDetailMessage>> GetChatPrivateDetailMessages(string userId, DateTime loginTime);

    /// <summary>
    /// 批量获取群聊聊天详情信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="loginTime">上次登录时间</param>
    /// <returns></returns>
    Task<List<ChatGroupDetailMessage>> GetChatGroupDetailMessages(string userId, DateTime loginTime);
}

internal class ChatRemoteService(
    IContainerProvider containerProvider,
    IMessageHelper messageHelper) : BaseService(containerProvider), IChatRemoteService
{
    public async Task<List<ChatGroupDetailMessage>> GetChatGroupDetailMessages(string userId, DateTime loginTime)
    {
        List<ChatGroupDetailMessage> result = new();

        // 构建请求对象
        var request = new GetGroupChatDetailListRequest
        {
            UserId = userId,
            PageIndex = 0,
            PageCount = 100,
            LastLoginTime = loginTime.ToString()
        };

        // 循环获取群聊详情信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GetGroupChatDetailListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Messages);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        return result;
    }

    public async Task<List<ChatPrivateDetailMessage>> GetChatPrivateDetailMessages(string userId, DateTime loginTime)
    {
        List<ChatPrivateDetailMessage> result = new();

        // 构建请求对象
        var request = new GetFriendChatDetailListRequest
        {
            UserId = userId,
            PageIndex = 0,
            PageCount = 100,
            LastLoginTime = loginTime.ToString()
        };

        // 循环获取群聊详情信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GetFriendChatDetailListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Messages);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        return result;
    }

    public async Task<List<FriendChatMessage>> GetFriendChatMessages(string userId, DateTime loginTime)
    {
        List<FriendChatMessage> result = new();

        // 构建请求对象
        var request = new GetFriendChatListRequest
        {
            UserId = userId,
            PageIndex = 0,
            PageCount = 100,
            LastLoginTime = loginTime.ToString()
        };

        // 循环获取群聊信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GetFriendChatListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Messages);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        return result;
    }

    public async Task<List<GroupChatMessage>> GetGroupChatMessages(string userId, DateTime loginTime)
    {
        List<GroupChatMessage> result = new();

        // 构建请求对象
        var request = new GetGroupChatListRequest
        {
            UserId = userId,
            PageIndex = 0,
            PageCount = 100,
            LastLoginTime = loginTime.ToString()
        };

        // 循环获取群聊信息，直到没有更多数据
        while (true)
        {
            var response = await messageHelper.SendMessageWithResponse<GetGroupChatListResponse>(request);
            if (response is { Response: { State: true } })
            {
                result.AddRange(response.Messages);
                if (response.HasNext)
                    request.PageIndex++;
                else
                    break;
            }
            else
                break;
        }

        return result;
    }
}