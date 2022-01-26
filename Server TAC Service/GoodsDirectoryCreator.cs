using System.Collections.Generic;
using System.Data;

namespace Server_TAC_Service
{
    class GoodsDirectoryCreator
    {
        public List<GoodsDirectory> goodsDirectory;
        private List<GoodsDirectory> subDirectory;

        public List<GoodsDirectory> Create(DataTable tbl)
        {
            int Code;
            int ParentCode;
            string Name;

            goodsDirectory = new List<GoodsDirectory>();

            foreach (DataRow row in tbl.Rows)
            {
                if (string.IsNullOrEmpty(row[6].ToString()))
                {
                    Code = int.Parse(row[8].ToString());
                    Name = row[9].ToString();

                    AddDirectory(goodsDirectory, Code, Name, 1, 0);
                }
                else if (string.IsNullOrEmpty(row[4].ToString()))
                {
                    Code = int.Parse(row[6].ToString());
                    Name = row[7].ToString();
                    subDirectory = AddDirectory(goodsDirectory, Code, Name, 1, 0);

                    ParentCode = Code;
                    Code = int.Parse(row[8].ToString());
                    Name = row[9].ToString();
                    AddDirectory(subDirectory, Code, Name, 2, ParentCode);
                }
                else if (string.IsNullOrEmpty(row[2].ToString()))
                {
                    Code = int.Parse(row[4].ToString());
                    Name = row[5].ToString();
                    subDirectory = AddDirectory(goodsDirectory, Code, Name, 1, 0);

                    ParentCode = Code;
                    Code = int.Parse(row[6].ToString());
                    Name = row[7].ToString();
                    AddDirectory(subDirectory, Code, Name, 2, ParentCode);

                    ParentCode = Code;
                    Code = int.Parse(row[8].ToString());
                    Name = row[9].ToString();
                    AddDirectory(subDirectory, Code, Name, 3, ParentCode);
                }
                else if (string.IsNullOrEmpty(row[0].ToString()))
                {
                    Code = int.Parse(row[2].ToString());
                    Name = row[3].ToString();
                    subDirectory = AddDirectory(goodsDirectory, Code, Name, 1, 0);

                    ParentCode = Code;
                    Code = int.Parse(row[4].ToString());
                    Name = row[5].ToString();
                    AddDirectory(subDirectory, Code, Name, 2, ParentCode);

                    ParentCode = Code;
                    Code = int.Parse(row[6].ToString());
                    Name = row[7].ToString();
                    AddDirectory(subDirectory, Code, Name, 3, ParentCode);

                    ParentCode = Code;
                    Code = int.Parse(row[8].ToString());
                    Name = row[9].ToString();
                    AddDirectory(subDirectory, Code, Name, 4, ParentCode);
                }
                else
                {
                    Code = int.Parse(row[0].ToString());
                    Name = row[1].ToString();
                    subDirectory = AddDirectory(goodsDirectory, Code, Name, 1, 0);

                    ParentCode = Code;
                    Code = int.Parse(row[2].ToString());
                    Name = row[3].ToString();
                    AddDirectory(subDirectory, Code, Name, 2, ParentCode);

                    ParentCode = Code;
                    Code = int.Parse(row[4].ToString());
                    Name = row[5].ToString();
                    AddDirectory(subDirectory, Code, Name, 3, ParentCode);

                    ParentCode = Code;
                    Code = int.Parse(row[6].ToString());
                    Name = row[7].ToString();
                    AddDirectory(subDirectory, Code, Name, 4, ParentCode);

                    ParentCode = Code;
                    Code = int.Parse(row[8].ToString());
                    Name = row[9].ToString();
                    AddDirectory(subDirectory, Code, Name, 5, ParentCode);
                }
            }
            return goodsDirectory;
        }
        private List<GoodsDirectory> AddDirectory(List<GoodsDirectory> directory, int Code, string Name, int Level, int ParentCode)
        {
            int Index = directory.FindIndex(x => x.Code == Code);
            if (Index == -1)
            {
                directory.Add(new GoodsDirectory()
                {
                    Code = Code,
                    ParentCode = ParentCode,
                    Name = Name,
                    Level = Level,
                    Child = new List<GoodsDirectory>()
                });
                Index = directory.FindIndex(x => x.Code == Code);
            }
            return directory[Index].Child;
        }
    }
}
