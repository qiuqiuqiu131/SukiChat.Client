namespace ChatClient.Tool.Data.File;

public class FileProcessDto : BindableBase
{
    public string FileName { get; set; }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _isSuccess;

    public bool IsSuccess
    {
        get => _isSuccess;
        set => SetProperty(ref _isSuccess, value);
    }

    private long maxSize;

    public long MaxSize
    {
        get => maxSize;
        set
        {
            if (SetProperty(ref maxSize, value))
                RaisePropertyChanged(nameof(Progress));
        }
    }

    private long currentSize;

    public long CurrentSize
    {
        get => currentSize;
        set
        {
            if (SetProperty(ref currentSize, value))
            {
                RaisePropertyChanged(nameof(Progress));
                // Console.WriteLine(
                //$"FileName:{FileName} Received New Pack, CurrentSize:{CurrentSize}, MaxSize:{MaxSize}, Progress:{Progress}");
            }
        }
    }

    public double Progress => (double)CurrentSize / MaxSize;
}