using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureBloblUploader
{
    class Program
    {
        static void Main(string[] args)
        {
            var manager = new BlobStorageManager("CloudStorageConnection");
            var result = manager.UploadDirectory(@"<path>", "$web");
            Console.WriteLine(result);
            Console.ReadKey();
        }
    }
}
