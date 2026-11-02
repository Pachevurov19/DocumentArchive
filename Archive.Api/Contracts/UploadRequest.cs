namespace Archive.Api.Contracts
{
    public class UploadRequest
    {
        public List<UploadFileInput> Files { get; set; } = [];

        public string FolderName { get; set; } = string.Empty;

        public string IndexFieldName { get; set; } = string.Empty;
    }
}
