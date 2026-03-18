namespace LiveMap.Web.Models.User
{
    public class UserViewModel
    {
        public Guid Id { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Followers { get; set; }

        public int Following { get; set; }
    }
}
