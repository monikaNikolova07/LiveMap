namespace LiveMap.Core.DTOs.Admin
{
    public class AdminDashboardDto
    {
        public List<AdminProfileItemDto> Profiles { get; set; } = new();
        public List<AdminFolderItemDto> Folders { get; set; } = new();
        public List<AdminPhotoItemDto> Photos { get; set; } = new();
    }

    public class AdminProfileItemDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePicture { get; set; }
        public int FoldersCount { get; set; }
    }

    public class AdminFolderItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
        public int PicturesCount { get; set; }
    }

    public class AdminPhotoItemDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public Guid FolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}
