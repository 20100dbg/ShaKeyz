using System;
using System.IO;

namespace ShaKeyz
{

    public enum DataType { FileSearch, FileResults, FileRequest, FileSend };

    [Serializable]
    public class Data
    {
        public DataType type;
        
        public Data Clone()
        {
            return (Data)MemberwiseClone();
        }

        public override String ToString()
        {
            return type.ToString();
        }
    }


    [Serializable]
    public class DataFileSearch : Data
    {
        public String search;

        public DataFileSearch() { base.type = DataType.FileSearch; }
    }

    [Serializable]
    public class DataFileResults : Data
    {
        public String[] results;

        public DataFileResults() { base.type = DataType.FileResults; }
    }

    [Serializable]
    public class DataFileRequest : Data
    {
        public String request;

        public DataFileRequest() { base.type = DataType.FileRequest; }
    }


    [Serializable]
    public class DataFileSend : Data
    {
        public sFile fileinfo;
        public Byte[] data = new Byte[Program.buffersize];
        public Int64 position;

        public DataFileSend() { base.type = DataType.FileSend; }

        public override String ToString()
        {
            return fileinfo.ToString();
        }
    }



    [Serializable]
    public class sFile
    {
        public String path;
        public String name;
        public Int64 size;

        String[] tabSize = { "o", "ko", "mo", "go", "to", "po" };


        public sFile(String path)
        {
            this.path = path;
            name = Path.GetFileName(path);
            size = new FileInfo(path).Length;
        }

        public String StringSize()
        {
            Double s = size;
            int x = 0;

            while (s > 1024 && x < tabSize.Length - 1)
            {
                s /= 1024;
                x++;
            }

            return Math.Round(s, 2) + tabSize[x];
        }

        public override string ToString()
        {
            return name + " (" + StringSize() + ")";
        }    
    }
}