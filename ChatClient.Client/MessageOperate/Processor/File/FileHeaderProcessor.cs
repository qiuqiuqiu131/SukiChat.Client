using File.Protobuf;

namespace ChatClient.MessageOperate.Processor.File;

public class FileHeaderProcessor : ProcessorBase<FileHeader>
{
    public FileHeaderProcessor(IContainerProvider container) : base(container)
    {
    }

    protected override async Task OnProcess(FileHeader message)
    {
        // 与FileManager交互
    }
}