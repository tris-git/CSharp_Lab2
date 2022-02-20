using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Lab2_CSharp
{
    struct DataItem {
        public Vector2 Point{get; set;}
        public Complex Value{get; set;}

        public DataItem(Vector2 point, Complex value)
        {
            Point = point;
            Value = value;
        }

        public string ToLongString(string format) => $"Point: {Point.ToString(format)} | Value: {Value.ToString(format)}";

        public override string ToString() => $"Point: {Point} | Value: {Value}";
    }

    public delegate Complex Fv2Complex(Vector2 v2);

    abstract class V2Data : IEnumerable<DataItem>
    {
        public string ID{get; protected set;}
        public DateTime Date{get; protected set;}

        public V2Data(string id, DateTime date)
        {
            ID = id;
            Date = date;
        }

        public abstract int Count{get;}
        public abstract float MinDistance{get;}

        public abstract string ToLongString(string fromat);
        public override string ToString() => $"ID: {ID} | Date: {Date}";

        public abstract IEnumerator<DataItem> GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    class V2DataList : V2Data
    {
        public List<DataItem> DList{get;}

        public V2DataList(string id, DateTime date) : base(id, date) 
        {
            DList = new List<DataItem>();
        }

        public bool Add(DataItem newItem) 
        {
            foreach(DataItem item in DList)
                if (item.Point == newItem.Point)
                    return false;
            
            DList.Add(newItem);
            return true;
        }

        public int AddDefaults(int nItems, Fv2Complex F) 
        {
            int cnt = 0;
            Random rand = new Random();
            for (int i = 0; i < nItems; i++)
            {
                Vector2 v = new Vector2((float)rand.Next(0, 10), (float)rand.Next(5, 15));
                DataItem di = new DataItem(v, F(v));
                if (Add(di))
                    cnt++;
            }
            return cnt;
        }

        public override int Count
        { get { return DList.Count; } }

        public override float MinDistance
        { 
            get {
                if (Count < 2)
                    return 0f;
                float min = Vector2.Distance(DList[0].Point, DList[1].Point);
                for (int i = 0; i < DList.Count; i++)
                    for (int j = i + 1; j < DList.Count; j++) 
                    {
                        float dist = Vector2.Distance(DList[i].Point, DList[j].Point);
                        if (dist < min)
                            min = dist;
                    }
                return min;
            }
        }

        public override string ToString() => $"Type: {(this.GetType()).ToString()} | {base.ToString()}"
            + $" | Count: {this.Count}";

        public override string ToLongString(string format)
        {
            string output = ToString() + "\nList items:";
            foreach (DataItem item in DList) output += $"\n{item.ToLongString(format)}";
            return output;
        }

        public override IEnumerator<DataItem> GetEnumerator()
        {
            return DList.GetEnumerator();            
        }

        public bool SaveAsText(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename, false))
            {
                sw.WriteLine(ID);
                sw.WriteLine(Date);
                foreach (DataItem di in DList)
                {
                    sw.WriteLine(di.Point.X);
                    sw.WriteLine(di.Point.Y);
                    sw.WriteLine(di.Value.Real);
                    sw.WriteLine(di.Value.Imaginary);
                }
            }

            return true;
        }

        public bool LoadAsText(string filename, ref V2DataList v2)
        {
            try {
                using (StreamReader sr = new StreamReader(filename, false))
                {
                    v2 = new V2DataList(sr.ReadLine(), DateTime.Parse(sr.ReadLine()));
                    string line = "";
                    while((line = sr.ReadLine()) != null)
                    {
                        Vector2 vec2 = new Vector2(Single.Parse(line), Single.Parse(sr.ReadLine()));
                        Complex c = new Complex(Double.Parse(sr.ReadLine()), Double.Parse(sr.ReadLine()));
                        v2.Add(new DataItem(vec2, c));
                    }
                }
            }
            catch (Exception e) 
            {
                Console.WriteLine($"The file could not be read:\n{e.Message}");
            }
            return true;
        }
    }

    class V2DataArray : V2Data
    {
        public int LabelsX{get;}
        public int LabelsY{get;}
        public Vector2 GridStep{get;}
        public Complex[,] GridValues{get; private set;}
        
        public V2DataArray(string id, DateTime date) : base(id, date)
        {
            GridValues = new Complex[0, 0];
        }
        
        public V2DataArray(string id, DateTime date, int nx, int ny, Vector2 gridStep, Fv2Complex F) : base(id, date)
        {
            LabelsX = nx;
            LabelsY = ny;
            GridStep = gridStep;
            GridValues = new Complex[nx, ny];
            if (F != null)
            {
                for (int i = 0; i < nx; i++)
                    for (int j = 0; j < ny; j++)
                        GridValues[i, j] = F(new Vector2(gridStep.X * i, gridStep.Y * j));
            }
        }

        public override int Count 
        { get { return LabelsX * LabelsY; } }

        public override float MinDistance
        {
            get {
                if (Count < 2)
                    return 0f;
                if (GridStep.X < GridStep.Y)
                    return GridStep.X;
                else
                    return GridStep.Y;
            }
        }
                
        public override string ToString() => $"Type: {(this.GetType()).ToString()} | {base.ToString()} |"
            + $" LabelsX: {LabelsX} | LabelsY: {LabelsY} | GridStep: {GridStep}";
        
        public override string ToLongString(string format) 
        {
            string output = ToString() + "\nArray items:";
            foreach (DataItem di in this) output += $"\n{di.ToLongString(format)}";
            return output;
        }

        public static explicit operator V2DataList(V2DataArray v2da)
        {
            V2DataList v2dl = new V2DataList(v2da.ID, v2da.Date);
            Vector2 gridStep = v2da.GridStep;
            for (int i = 0; i < v2da.LabelsX; i++)
                for (int j = 0; j < v2da.LabelsY; j++)
                    v2dl.Add(new DataItem(new Vector2(i * gridStep.X, j * gridStep.Y),
                        v2da.GridValues[i, j]));
            return v2dl;
        }

        public override IEnumerator<DataItem> GetEnumerator()
        {
            return ((V2DataList) this).GetEnumerator();
        }

        public bool SaveBinary(string filename)
        {
            using (var stream = File.Open(filename, FileMode.Create))
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                {
                    writer.Write(ID);
                    writer.Write(Date.ToBinary());
                    writer.Write(LabelsX);
                    writer.Write(LabelsY);
                    writer.Write(GridStep.X);
                    writer.Write(GridStep.Y);
                    foreach (DataItem di in this)
                    {
                        writer.Write(di.Value.Real);
                        writer.Write(di.Value.Imaginary);
                    }
                }
            }

            return true;
        }

        public bool LoadBinary(string filename, ref V2DataArray v2)
        {
            string id;
            DateTime dt;
            int nx;
            int ny;
            Vector2 gs;
            try {
                using (var stream = File.Open(filename, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
                    {
                        id = reader.ReadString();
                        dt = DateTime.FromBinary(reader.ReadInt64());
                        nx = reader.ReadInt32();
                        ny = reader.ReadInt32();
                        gs = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                        Complex[,] gvs = new Complex[nx, ny];
                        for (int i = 0; i < nx; i++)
                            for (int j = 0; j < ny; j++) 
                                gvs[i, j] = new Complex(reader.ReadDouble(), reader.ReadDouble());
                        v2 = new V2DataArray(id, dt, nx, ny, gs, null);
                        v2.GridValues = gvs;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"The file could not be read:\n{e.Message}");   
            }

            return true;
        }
    }
    
    class V2MainCollection
    {
        private List<V2Data> V2DList;

        public V2MainCollection()
        {
            V2DList = new List<V2Data>();
        }

        public int Count
        { get { return V2DList.Count; } }

        public V2Data this[int index]
        { get { return V2DList[index]; } }

        public float MinDistance
        { 
            get
            {
                if (Count == 0)
                    return float.NaN;
                return (from v2dlelement in V2DList select v2dlelement).Min(v2dlelement => v2dlelement.MinDistance);
            } 
        }

        public IEnumerable<Vector2> AllPoints
        {
            get
            {
                if (Count == 0)
                    return null;
                var points = (from v2dlelement in V2DList
                                let v2dlist = v2dlelement
                                from dataitem in v2dlist select dataitem.Point).Distinct();
                if (!points.Any())
                    return null;
                return points;
            }
        }

        public IEnumerable<V2DataList> SpecialProp
        {
            get
            {
                if (Count == 0)
                    return null;
                var query1 = from v2dlelement in V2DList
                            where v2dlelement is V2DataList && v2dlelement.Count > 0
                            select v2dlelement as V2DataList;
                var query2 = from v2dlist in query1           
                            let v2dl = v2dlist
                            from dataitem in v2dl 
                            where dataitem.Value.Imaginary == 0
                            select v2dlist;
                var query3 = query1.Except(query2);
                if (!query3.Any())
                    return null;
                return query3;
            }
        }

        public bool Contains(string ID)
        {
            foreach (V2Data node in V2DList) 
            {
                if (String.Equals(node.ID, ID))
                    return true;
            }
            return false;
        }

        public bool Add(V2Data v2Data)
        {
            if (Contains(v2Data.ID))
                return false;
            V2DList.Add(v2Data);
            return true;
        }

        public string ToLongString(string format) 
        {
            string output = "V2MainCollection elements:";
            foreach (V2Data node in V2DList) output += $"\n{node.ToLongString(format)}";
            return output;
        }
        
        public override string ToString()
        {
            string output = "V2MainCollection elements:";
            foreach (V2Data node in V2DList) output += $"\n{node.ToString()}";
            return output;
        }
    }
    
    static class Methods
    {
        public static Complex F1(Vector2 v2)
        { return new Complex(v2.X + v2.Y, v2.X - v2.Y); }

        public static Complex F2(Vector2 v2)
        { return new Complex(v2.X * v2.Y, 4 * v2.X); }
    }

    class Program
    {
        static void Method1()
        {
            Vector2 v = new Vector2(1, 6);
            Vector2 v2 = new Vector2(3, 4);
            Complex c = new Complex(6, 2);
            Complex c2 = new Complex(7, 0);
            DataItem di = new DataItem(v, c);
            DataItem di2 = new DataItem(v2, c2);

            V2DataArray a = new V2DataArray("arrname", DateTime.Now, 2, 2, v2, Methods.F1);
            V2DataArray a2 = new V2DataArray("arrname2", DateTime.Now, 3, 4, v, Methods.F2);
            V2DataList l = (V2DataList) a;
            V2DataList l2 = new V2DataList("listname", DateTime.Now);
            l2.Add(di); 
            l2.Add(di2);

            Console.WriteLine($"Initial array:\n{a2.ToLongString(null)}");
            if (a.SaveBinary("BinFile.dat"))
                a.LoadBinary("BinFile.dat", ref a2);
            Console.WriteLine($"Readed array:\n{a2.ToLongString(null)}\n");

            Console.WriteLine($"Initial list:\n{l2.ToLongString(null)}");
            if (l.SaveAsText("TextFile.txt"))
                l.LoadAsText("TextFile.txt", ref l2);
            Console.WriteLine($"Readed list:\n{l2.ToLongString(null)}\n");
        }

        static void Method2()
        {
            Vector2 v = new Vector2(1, 6);
            Vector2 v2 = new Vector2(3, 4);
            Complex c = new Complex(6, 2);
            Complex c2 = new Complex(7, 0);
            DataItem di = new DataItem(v, c);
            DataItem di2 = new DataItem(v2, c2);

            V2DataArray a = new V2DataArray("arrname", DateTime.Now, 2, 2, v2, Methods.F1);
            V2DataArray a2 = new V2DataArray("arrname2", DateTime.Now, 3, 4, v, Methods.F2);
            V2DataList l = new V2DataList("listname", DateTime.Now);
            l.Add(di);
            V2DataList l2 = new V2DataList("listname2", DateTime.Now);
            l2.Add(di); 
            l2.Add(di2);
            V2DataList l3 = new V2DataList("emptylist", DateTime.Now);
            V2DataArray a3 = new V2DataArray("emptyarray", DateTime.Now);
            
            V2MainCollection mc = new V2MainCollection();
            mc.Add(a);
            mc.Add(a2);
            mc.Add(a3);
            mc.Add(l);
            mc.Add(l2);
            mc.Add(l3);
            Console.WriteLine($"Main collection elements count: {mc.Count}");

            Console.WriteLine("All elements min distance:");
            for (int i = 0; i < mc.Count; i++)
            {
                Console.WriteLine($"{mc[i].MinDistance}");
            }
            Console.WriteLine($"Min of min distances: {mc.MinDistance}\n");

            if (mc.AllPoints != null) {
                Console.WriteLine("All points in collection:");
                foreach (Vector2 point in mc.AllPoints)
                    Console.WriteLine(point);
            }

            if (mc.SpecialProp != null) {
                Console.WriteLine("Special prop:");
                foreach (V2Data v2d in mc.SpecialProp)
                    Console.WriteLine(v2d.ToLongString(null));
            }
        }

        static void Main(string[] args)
        {
            Program.Method1();
            Program.Method2();
        }
    }
}
