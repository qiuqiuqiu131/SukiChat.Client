using ChatServer.Common.Protobuf;
using File.Protobuf;
using SocketClient.Client;
using SocketClient.IOServer.Handler;
using SocketClient.MessageOperate;
using SocketClient.MessageOperate.Processor.Base;
using SocketClient.MessageOperate.Processor.Chat;
using SocketClient.MessageOperate.Processor.File;
using SocketClient.MessageOperate.Processor.FriendRelation;
using SocketClient.MessageOperate.Processor.Group;
using SocketClient.MessageOperate.Processor.GroupRelation;
using SocketClient.MessageOperate.Processor.Relation;
using SocketClient.MessageOperate.Processor.User;
using SocketClient.MessageOperate.Processor.WebRtc;

namespace SocketClient;

public static class ClientExtenstions
{
    /// <summary>
    /// 将实现了 IProcessor<> 接口的类注册到依赖注入容器中
    /// </summary>
    /// <param name="services"></param>
    internal static void AddProcessors(this IContainerRegistry containerRegistry)
    {
        #region 方式一：通过反射获取所有实现了 IProcessor<> 接口的类

        //// 获取基类 ProcessorBase<> 的类型
        //var processorBaseType = typeof(ProcessorBase<>);
        //// 获取当前执行程序集中的所有类型，并筛选出继承了 ProcessorBase<> 基类的类
        //var types = Assembly.GetExecutingAssembly().GetTypes()
        //    .Where(t => t.BaseType != null && t.BaseType.IsGenericType &&
        //                t.BaseType.GetGenericTypeDefinition() == processorBaseType
        //                && t.IsClass && !t.IsAbstract).ToList();

        //// 遍历筛选出的类型
        //foreach (var type in types)
        //{
        //    // 获取该类型的基类 ProcessorBase<> 的泛型参数
        //    var genericArgument = type.BaseType.GetGenericArguments().First();
        //    // 构建 IProcessor<> 接口类型
        //    var interfaceType = typeof(IProcessor<>).MakeGenericType(genericArgument);
        //    // 将接口和实现类注册到依赖注入容器中
        //    containerRegistry.RegisterScoped(interfaceType, type);
        //}

        #endregion

        #region 方式二：直接注册具体的处理器类

        // Base 处理器
        containerRegistry.RegisterScoped<IProcessor<CommonResponse>, CommonResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<HeartBeat>, HeartBeatProcessor>();

        // Chat 处理器
        containerRegistry.RegisterScoped<IProcessor<ChatGroupDeleteResponse>, ChatGroupDeleteResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<ChatGroupRetractMessage>, ChatGroupRetractMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<ChatPrivateDeleteResponse>, ChatPrivateDeleteResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<ChatPrivateRetractMessage>, ChatPrivateRetractMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<ChatShareMessageResponse>, ChatShareMessageResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<FriendChatMessageList>, FriendChatMessageListProcessor>();
        containerRegistry.RegisterScoped<IProcessor<FriendChatMessage>, FriendChatMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<FriendChatMessageResponse>, FriendChatMessageResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<FriendWritingMessage>, FriendWritingMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GroupChatMessageList>, GroupChatMessageListProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GroupChatMessage>, GroupChatMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GroupChatMessageResponse>, GroupChatMessageResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<UpdateFriendLastChatIdResponse>, UpdateFriendLastChatIdProcessor>();
        containerRegistry.RegisterScoped<IProcessor<UpdateGroupLastChatIdResponse>, UpdateGroupLastChatIdProcessor>();

        // File 处理器
        containerRegistry.RegisterScoped<IProcessor<FilePack>, FilePackProcessor>();
        containerRegistry.RegisterScoped<IProcessor<FileHeader>, FileHeaderProcessor>();

        // FriendRelation 处理器
        containerRegistry.RegisterScoped<IProcessor<DeleteFriendMessage>, DeleteFriendMessageProcessor>();
        containerRegistry
            .RegisterScoped<IProcessor<FriendRequestFromClientResponse>, FriendRequestFromClientResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<FriendRequestFromServer>, FriendRequestProcessor>();
        containerRegistry
            .RegisterScoped<IProcessor<FriendResponseFromClientResponse>, FriendResponseFromClientResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<FriendResponseFromServer>, FriendResponeseProcessor>();

        // Group 处理器
        containerRegistry.RegisterScoped<IProcessor<CreateGroupResponse>, CreateGroupResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<DisbandGroupMessage>, DisbandGroupMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GroupMemberIds>, GroupMemberIdsProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GroupMemberListResponse>, GroupMemberListResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GroupMemberMessage>, GroupMemberMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GroupMemeberRemovedMessage>, GroupMemberRemovedMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GroupMessageListResponse>, GroupMessageListResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GroupMessage>, GroupMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<QuitGroupMessage>, QuitGroupMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<RemoveMemberMessage>, RemoveMemberMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<ResetHeadImageResponse>, ResetHeadImageResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<UpdateGroupMessage>, UpdateGroupMessageProcessor>();

        // GroupRelation 处理器
        containerRegistry.RegisterScoped<IProcessor<JoinGroupRequestFromServer>, JoinGroupRequestFromServerProcessor>();
        containerRegistry
            .RegisterScoped<IProcessor<JoinGroupRequestResponseFromServer>,
                JoinGroupRequestResponseFromServerProcessor>();
        containerRegistry
            .RegisterScoped<IProcessor<JoinGroupResponseFromServer>, JoinGroupResponseFromServerProcessor>();
        containerRegistry
            .RegisterScoped<IProcessor<JoinGroupResponseResponseFromServer>,
                JoinGroupResponseResponseFromServerProcessor>();
        containerRegistry.RegisterScoped<IProcessor<PullGroupMessage>, PullGroupMessageProcessor>();

        // Relation 处理器
        containerRegistry.RegisterScoped<IProcessor<NewFriendMessage>, NewFriendMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<NewMemberJoinMessage>, NewMemberJoinMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<UpdateFriendRelation>, UpdateFriendRelationProcessor>();
        containerRegistry.RegisterScoped<IProcessor<UpdateGroupRelation>, UpdateGroupRelationProcessor>();

        // User 处理器
        containerRegistry.RegisterScoped<IProcessor<AddUserGroupResponse>, AddUserGroupResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<DeleteUserGroupResponse>, DeleteUserGroupResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<ForgetPasswordResponse>, ForgetPasswordResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<FriendLoginMessage>, FriendLoginMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<FriendLogoutMessage>, FriendLogoutMessageProcessor>();
        containerRegistry
            .RegisterScoped<IProcessor<GetFriendChatDetailListResponse>, GetFriendChatDetailListProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GetFriendChatListResponse>, GetFriendChatListResponseProcessor>();
        containerRegistry
            .RegisterScoped<IProcessor<GetGroupChatDetailListResponse>, GetGroupChatDetailListResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GetGroupChatListResponse>, GetGroupChatListResponseProcessor>();
        containerRegistry
            .RegisterScoped<IProcessor<GetUserDetailMessageResponse>, GetUserDetailMessageResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GetUserListResponse>, GetUserListResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<GetUserResponse>, GetUserMessageResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<LoginResponse>, LoginResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<LogoutCommand>, LogoutCommandProcessor>();
        containerRegistry.RegisterScoped<IProcessor<OutlineMessageResponse>, OutlineMessageResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<RegisteResponse>, RegisteResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<RenameUserGroupResponse>, RenameUserGroupResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<ResetPasswordResponse>, ResetPasswordResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<SearchGroupResponse>, SearchGroupResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<SearchUserResponse>, SearchUserResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<UpdateEmailNumberResponse>, UpdateEmailNumberResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<UpdatePhoneNumberResponse>, UpdatePhoneNumberResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<UpdateUserDataResponse>, UpdateUserDataResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<UserMessage>, UserMessageProcessor>();
        containerRegistry
            .RegisterScoped<IProcessor<PasswordAuthenticateResponse>, PasswordAuthenticateResponseProcessor>();

        // WebRtc 处理器
        containerRegistry.RegisterScoped<IProcessor<AudioStateChanged>, AudioStateChangedProcessor>();
        containerRegistry.RegisterScoped<IProcessor<CallRequest>, CallRequestProcessor>();
        containerRegistry.RegisterScoped<IProcessor<CallResponse>, CallResponseProcessor>();
        containerRegistry.RegisterScoped<IProcessor<HangUp>, HangUpProcessor>();
        containerRegistry.RegisterScoped<IProcessor<SignalingMessage>, SignalingMessageProcessor>();
        containerRegistry.RegisterScoped<IProcessor<VideoStateChanged>, VideoStateChangedProcessor>();

        #endregion
    }

    /// <summary>
    /// 配置客户端Handler处理器
    /// </summary>
    /// <param name="builder"></param>
    internal static SocketClientBuilder HandlerInit(this SocketClientBuilder builder)
    {
        builder.AddHandler<ClientConnectHandler>();
        builder.AddHandler<EchoClientHandler>();
        return builder;
    }
}