using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaEaterMMO.Shared.DTO.Game
{
    public class ScoreDTO
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int Value { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
