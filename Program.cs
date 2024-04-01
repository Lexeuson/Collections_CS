﻿using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using static LAB1.Program;

namespace LAB1
{
    public static class Program
    {
        public delegate void FValues(double x, ref double y1, ref double y2);
        public delegate DataItem FDI(double x);
        public struct DataItem
        {
            public double x;
            public double[] Y { get; set; }
            public DataItem(double x, double y1, double y2)
            {
                this.x = x;
                Y = new double[2];
                Y[0] = y1;
                Y[1] = y2;
            }
            public override readonly string ToString()
            {
                return string.Format("{0:d3} - ({1:d3}, {2:d3})", x, Y[1], Y[2]);
            }
            public readonly string ToLongString(string format)
            {
                return string.Format("{0:" + format + "} - [{1:" + format + "}, {2:" + format + "}]", x, Y[0], Y[1]);
            }
        }

        abstract class V2Data
        {
            public string Key {  get; set; }
            public DateTime Time { get; set; }
            public abstract double MinField { get; }
            public V2Data(string key, DateTime time)
            {
                Key = key;
                Time = time;
            }
            public abstract string ToLongString(string format);
            public override string ToString()
            {
                return Key + " | " + Time;
            }
            public abstract double yAverageForXMax { get; }
        }
        class V2DataList: V2Data
        {
            public List<DataItem> DataItems { get; set; }
            bool Contains(double x)
            {
                foreach(DataItem dataItem in DataItems)
                {
                    if(dataItem.x == x) return true;
                }
                return false;
            }
            public V2DataList(string key, DateTime time): base(key, time) { DataItems = new List<DataItem>(); }
            public V2DataList(string key, DateTime time, double[] x, FDI f): base(key, time)
            {
                DataItems = new List<DataItem>();
                foreach (double x2 in x)
                {
                    if(!Contains(x2))
                        DataItems.Add(f(x2));
                }
            }
            public V2DataList(string key, DateTime time, double[] x, double[,] y) : base(key, time)
            {
                DataItems = new List<DataItem>();
                for (int i = 0; i < x.Length; i++)
                {
                    if (!Contains(x[i]))
                        DataItems.Add(new DataItem(x[i], y[0, i], y[1, i]));
                }
            }
            public override double MinField
            {
                get
                {
                    System.Double min = System.Double.MaxValue;
                    foreach(DataItem item in DataItems)
                    {
                        if (Math.Abs(item.Y[0]) < min) { min = Math.Abs(item.Y[0]); }
                        if (Math.Abs(item.Y[1]) < min) { min = Math.Abs(item.Y[1]); }
                    }
                    return DataItems.Count == 0? -1: min;
                }
            }
            public override string ToString()
            {
                return base.ToString();
            }
            public override string ToLongString(string format)
            {
                string result = string.Empty;
                foreach(DataItem item in DataItems)
                {
                    result += "\t" + item.ToLongString(format) + "\n";
                }
                return ToString() + ":\n" + result;
            }
            public V2DataArray ToV2DataArray {  
                get
                {
                    double[] x = new double[DataItems.Count];
                    double[,] y = new double[2, DataItems.Count];
                    for(int i = 0; i < DataItems.Count; i++)
                    {
                        x[i] = DataItems[i].x;
                        y[0, i] = DataItems[i].Y[0];
                        y[1, i] = DataItems[i].Y[1];
                    }
                    return new V2DataArray(this.Key, this.Time, x, y);
                } 
            }
            public override double yAverageForXMax { 
                get {
                    double result = -1;
                    System.Double xMax = System.Double.MinValue;
                    foreach(DataItem dataItem in DataItems)
                    {
                        if(xMax < dataItem.x)
                        {
                            xMax = dataItem.x;
                            result = (dataItem.Y[0] + dataItem.Y[1])/2;
                        }
                    }
                    return result;
                } 
            }
        }
        class V2DataArray: V2Data
        {
            double[] X {  get; set; }
            double[,] Y { get; set; }
            public V2DataArray(string key, DateTime time): base(key, time)
            {
                X = Array.Empty<double>();
                Y = new double[2,0];
            }
            public V2DataArray(string key, DateTime time, double[] x, FValues f): base(key, time)
            {
                X = new double[x.Length];
                Y = new double[2, x.Length];
                for(int i = 0; i < x.Length; i++)
                {
                    X[i] = x[i];
                    f(x[i], ref Y[0, i], ref Y[1, i]);
                }
            }
            public V2DataArray(string key, DateTime time, double[] x, double[,] y) : base(key, time)
            {
                X = new double[x.Length];
                Y = new double[2, x.Length];
                for (int i = 0; i < x.Length; i++)
                {
                    X[i] = x[i];
                    Y[0, i] = y[0, i];
                    Y[1, i] = Y[1, i];
                }
            }
            public V2DataArray(string key, DateTime time, int nX, double xL, double xR, FValues f): base(key, time)
            {
                X = new double[nX];
                Y = new double[2, nX];
                for(int i = 0; i < nX; i++)
                {
                    X[i] = xL + i*(xR - xL)/nX;
                    f(X[i], ref Y[0, i], ref Y[1, i]);
                }
            }
            public double[]? this[int index]
            {
                get 
                {
                    if (index < 0 || index > 1) return null;// normally would be handled by exception
                    return Enumerable.Range(0, Y.GetLength(1)).Select(t => Y[index, t]).ToArray();
                }
            }
            public static explicit operator V2DataList(V2DataArray source)
            {
                return new V2DataList(source.Key, source.Time, source.X, source.Y);
            }
            public override double MinField
            {
                get
                {
                    System.Double min = System.Double.MaxValue;
                    foreach (double y in Y)
                    {
                        if (Math.Abs(y) < min) { min = Math.Abs(y); }
                    }
                    return Y.Length == 0 ? -1 : min;
                }
            }
            public override string ToString()
            {
                return base.ToString();
            }
            public override string ToLongString(string format)
            {
                string result = string.Empty;
                for (int i = 0; i < X.Length; i++)
                {
                    result += "\t" + string.Format("{0:" + format + "} - [{1:" + format + "}, {2:" + format + "}]", X[i], Y[0,i], Y[1,i]) + "\n";
                }
                return ToString() + ":\n" + result;
            }
            public override double yAverageForXMax
            {
                get
                {
                    double result = -1;
                    System.Double xMax = System.Double.MinValue;
                    for(int i = 0; i < X.Length; i++)
                    {
                        if (xMax < X[i])
                        {
                            xMax = X[i];
                            result = (Y[0, i] + Y[1, i]) / 2;
                        }
                    }
                    return result;
                }
            }
        }
        class V2DataArray1 : V2Data
        {
            double[] datas;
            public V2DataArray1(string key, DateTime time, double[] x, FValues f) : base(key, time)
            {
                double y1 = 0, y2 = 0;
                this.datas = new double[3*x.Length];
                for (int i = 0; i < x.Length; i++)
                {
                    f(x[i], ref y1, ref y2);
                    this.datas[3*i] = x[i];
                    this.datas[3 * i + 1] = y1;
                    this.datas[3 * i + 2] = y2;
                }
            }
            public override double MinField
            {
                get
                {
                    return 0;
                }
            }
            public override string ToString()
            {
                return this.GetType() + " - " +base.ToString();
            }
            public override string ToLongString(string format)
            {
                string result = string.Empty;
                for (int i = 0; i < datas.Length/3; i++)
                {
                    result += "\t" + string.Format("{0:" + format + "} - [{1:" + format + "}, {2:" + format + "}]", datas[3*i], datas[3*i + 1], datas[3*i + 2]) + "\n";
                }
                return ToString() + ":\n" + result;
            }
            public override double yAverageForXMax
            {
                get
                {
                    double result = -1;
                    System.Double xMax = System.Double.MinValue;
                    for (int i = 0; i < datas.Length/3; i++)
                    {
                        if (xMax < datas[3*i])
                        {
                            xMax = datas[3*i];
                            result = (datas[3*i + 1] + datas[3*i + 2]) / 2;
                        }
                    }
                    return result;
                }
            }
        }
        class V2MainCollection: System.Collections.ObjectModel.ObservableCollection<V2Data>
        {
            bool Contains(string key)
            {
                foreach(var item in Items)
                {
                    if(item.Key == key) return true;
                }
                return false;
            }
            public new bool Add(V2Data v2Data)
            {
                if(this.Contains(v2Data.Key)) return false;
                Items.Add(v2Data);
                return true;
            }
            public V2MainCollection(int nV2DataArray, int nV2DataList)
            {
                Random randObj = new();
                for(int i = 0; i < nV2DataArray; i++)
                {
                    double t = randObj.NextDouble();
                    Items.Add(new V2DataArray($"Array 1-{i}", DateTime.Now, randObj.Next(2, 10), t * 10, (randObj.NextDouble() + t) * 10, SetFValues));
                }
                for(int i = 0;i < nV2DataList; i++)
                {
                    int length = randObj.Next(2, 6);
                    double[] x = new double[length];
                    for(int j = 0; j < x.Length; j++)
                    {
                        x[j] = randObj.NextDouble()*11;
                    }
                    Items.Add(new V2DataList($"List 2-{i}", DateTime.Now, x, CreateDataItem));
                }
            }
            public override string ToString()
            {
                string result = string.Empty;
                foreach(V2Data v2Data in Items) { result += v2Data.ToString() + "\n"; }
                return result;
            }
            public string ToLongString(string format)
            {
                string result = "=============================================\n";
                foreach (V2Data v2Data in Items)
                {
                    result += v2Data.ToLongString(format) + "\n" + "---------------------------------------------" + "\n";
                }
                return result;
            }
            public V2MainCollection()
            {
                Random randObj = new();
                double t = randObj.NextDouble();
                Items.Add(new V2DataArray($"Array 1-1", DateTime.Now, randObj.Next(2, 10), t * 10, (randObj.NextDouble() + t) * 10, SetFValues));
                
                int length = randObj.Next(2, 6);
                double[] x = new double[length];
                for (int j = 0; j < x.Length; j++)
                {
                    x[j] = randObj.NextDouble() * 11;
                }
                Items.Add(new V2DataList($"List 2-1", DateTime.Now, x, CreateDataItem));

                length = randObj.Next(2, 6);
                x = new double[length];
                for (int j = 0; j < x.Length; j++)
                {
                    x[j] = randObj.NextDouble() * 11;
                }
                Items.Add(new V2DataArray1("Array1 3-1", DateTime.Now, x, SetFValues));
            }
        }
        public static void SetFValues(double x, ref double y1, ref double y2)
        {
            y1 = -x;
            y2 = x + 1;
        }
        public static DataItem CreateDataItem(double x)
        {
            return new DataItem(x, x, x + 1);
        }
        public static void Main()
        {
            V2DataArray v2DataArray = new("Test Array", DateTime.Now, 6, -8.0, -2.0, SetFValues);
            Console.WriteLine(v2DataArray.ToLongString("N3"));
            V2DataList v2DataList = (V2DataList)v2DataArray;
            Console.WriteLine(v2DataList.ToLongString("N3"));
            V2DataList v2DataList1 = new("Test List", DateTime.Now, new double[3] { 1, 2, 3 }, CreateDataItem);
            Console.WriteLine(v2DataList1.ToLongString("N3"));
            Console.WriteLine(v2DataList1.ToV2DataArray.ToLongString("N3"));
            V2MainCollection v2Datas = new(2, 2);
            Console.WriteLine(v2Datas.ToLongString("N3"));
            foreach(var v2Data in v2Datas)
            {
                Console.WriteLine(v2Data + " - " + v2Data.MinField);
            }
            /*V2MainCollection v2Datas1 = new V2MainCollection();
            Console.WriteLine(v2Datas1.ToLongString("N3"));
            foreach (var v2Data in v2Datas1)
            {
                Console.WriteLine(v2Data + " - " + v2Data.yAverageForXMax);
            }*/
        }
    }
}