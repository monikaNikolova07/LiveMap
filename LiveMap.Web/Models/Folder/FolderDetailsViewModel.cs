using LiveMap.Core.DTOs.Folders;
using LiveMap.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace LiveMap.Web.Models.Folder
{
    public class FolderDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Acssesability Acssesability { get; set; }
        public bool IsOwner { get; set; }
        public Guid OwnerProfileId { get; set; }
        public Guid? ParentFolderId { get; set; }
        public List<FolderPictureItemViewModel> Pictures { get; set; } = new();
        public List<FolderChildItemViewModel> Subfolders { get; set; } = new();
        public FolderCreateDto CreateSubfolder { get; set; } = new() { IsCountryFolder = false };
        public FolderPictureUploadViewModel UploadPicture { get; set; } = new();
    }

    public class FolderPictureItemViewModel
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public Acssesability Acssesability { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public bool CanInteract { get; set; }
        public List<PictureCommentItemViewModel> Comments { get; set; } = new();
    }

    public class PictureCommentItemViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }

    public class FolderChildItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Acssesability Acssesability { get; set; }
        public int PicturesCount { get; set; }
    }

    public class FolderPictureUploadViewModel
    {
        [Required]
        public Guid FolderId { get; set; }

        [Required]
        public IFormFile? File { get; set; }

        public Acssesability Acssesability { get; set; } = Acssesability.Public;
    }
}
