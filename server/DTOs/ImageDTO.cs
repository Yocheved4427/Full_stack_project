using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DTOs
{
    public record ImageDTO
    (
        int ImageId,

        int ProductId,

        string Url,

        bool IsMain
    );
}
