using File.Protobuf;

namespace SocketClient.MessageOperate.Processor.File;

public class FilePackProcessor : ProcessorBase<FilePack>
{
    public FilePackProcessor(IContainerProvider container) : base(container)
    {
    }

    protected override async Task OnProcess(FilePack message)
    {
        // 与FileManager交互
    }
}