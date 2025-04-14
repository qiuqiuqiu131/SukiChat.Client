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
            ChatMessage.ContentOneofCase.SystemMessage => context.Mapper.Map<SystemMessDto>(source.SystemMessage),
            ChatMessage.ContentOneofCase.CardMess => context.Mapper.Map<CardMessDto>(source.CardMess),
            ChatMessage.ContentOneofCase.VoiceMess => context.Mapper.Map<VoiceMessDto>(source.VoiceMess),
            ChatMessage.ContentOneofCase.CallMess => context.Mapper.Map<CallMessDto>(source.CallMess),
        };
    }
}