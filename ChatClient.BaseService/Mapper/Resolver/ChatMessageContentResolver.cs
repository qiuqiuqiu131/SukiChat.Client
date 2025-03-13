using AutoMapper;
using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Mapper.Resolver;

internal class ChatMessageContentResolver : IValueResolver<ChatMessage, ChatMessageDto, object>
{
    public object Resolve(ChatMessage source, ChatMessageDto destination, object destMember, ResolutionContext context)
    {
        return source.ContentCase switch
        {
            ChatMessage.ContentOneofCase.TextMess => context.Mapper.Map<TextMessDto>(source.TextMess),
            ChatMessage.ContentOneofCase.ImageMess => context.Mapper.Map<ImageMessDto>(source.ImageMess),
            ChatMessage.ContentOneofCase.FileMess => context.Mapper.Map<FileMessDto>(source.FileMess),
            ChatMessage.ContentOneofCase.SystemMessage => context.Mapper.Map<SystemMessDto>(source.SystemMessage)
        };
    }
}