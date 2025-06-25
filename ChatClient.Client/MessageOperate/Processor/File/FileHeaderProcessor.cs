using File.Protobuf;

namespace SocketClient.MessageOperate.Processor.File;

public class FileHeaderProcessor(IContainerProvider container) : ProcessorBase<FileHeader>(container)
{
    protected override async Task OnProcess(FileHeader message)
    {
        // 与FileManager交互
    }
}