using System;

namespace Entities.Response
{
    public class ResOnvoCustomer : ResBase
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}