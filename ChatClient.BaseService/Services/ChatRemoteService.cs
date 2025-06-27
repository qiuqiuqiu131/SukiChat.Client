using ChatClient.BaseService.Services.Interface.RemoteService;
using ChatClient.Tool.HelperInterface;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Services;

internal class ChatRemoteService(
    IContainerProvider containerProvider,
    IMessageHelper messageHelper) : Services.BaseService(containerProvider), IChatRemoteService
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
                throw new Exception("聊天记录获取失败");
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
                throw new Exception("聊天记录获取失败");
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
                throw new Exception("聊天记录获取失败");
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
                throw new Exception("聊天记录获取失败");
        }

        return result;
    }
}