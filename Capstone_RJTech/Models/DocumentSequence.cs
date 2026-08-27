using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models
{
    public class DocumentSequence
    {
        [Key, StringLength(100)]
        public string sequence_name { get; set; } = string.Empty;

        public int last_value { get; set; }
    }
}
