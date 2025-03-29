using File.Protobuf;

namespace ChatClient.MessageOperate.Processor.Chat;

public class FileHeaderProcessor(IContainerProvider container) : ProcessorBase<FileHeader>(container);