using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace ChatServer.Common.Tool
{
    internal class ProtobufDto
    {
        public string? FullName { get; set; }
        public Type? Type { get; set; }
    }

    public class ProtobufHelper
    {
        private static List<ProtobufDto> _registery = new List<ProtobufDto>();

        static ProtobufHelper()
        {
            _registery = new List<ProtobufDto>();

            // 获取当前程序集下所有的类型
            var q = from t in Assembly.GetExecutingAssembly().GetTypes()
                    select t;
            q.ToList().ForEach(t =>
            {
                // 如果是IMessage的子类
                if (typeof(IMessage).IsAssignableFrom(t))
                {
                    // 获取Descriptor属性
                    var desc = t.GetProperty("Descriptor")!.GetValue(t) as MessageDescriptor;
                    _registery.Add(new ProtobufDto { FullName = desc!.FullName, Type = t });
                }
            });

            // 按照FullName排序
            _registery.Sort((x, y) =>
            {
                //按照字符串长度排序，
                if (x.FullName!.Length != y.FullName!.Length)
                {
                    return x.FullName.Length - y.FullName.Length;
                }
                //如果长度相同
                return string.Compare(x.FullName, y.FullName, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// 获取IMessage序号
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int SeqCode(Type type)
        {
            return _registery.FindIndex(x => x.Type == type);
        }

        /// <summary>
        /// 获取序号对应类型
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static Type SeqType(int code)
        {
            return _registery[code].Type!;
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static byte[] Serialize(IMessage msg)
        {
            using (MemoryStream rawOutput = new MemoryStream())
            {
                int seq = SeqCode(msg.GetType());
                // 将序号写入流
                rawOutput.Write(BitConverter.GetBytes((ushort)seq), 0, 2);
                // 将IMessage写入流
                msg.WriteTo(rawOutput);
                byte[] result = rawOutput.ToArray();
                return result;
            }
        }

        /// <summary>
        /// 解析
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataBytes"></param>
        /// <returns></returns>
        public static IMessage ParseFrom(byte[] dataBytes)
        {
            using (MemoryStream rawInput = new MemoryStream(dataBytes))
            {
                // 读取序号
                byte[] seqBytes = new byte[2];
                rawInput.Read(seqBytes, 0, 2);
                ushort seq = BitConverter.ToUInt16(seqBytes, 0);
                Type type = SeqType(seq);
                // 读取IMessage
                IMessage msg = (IMessage)Activator.CreateInstance(SeqType(seq))!;
                msg.MergeFrom(rawInput);
                return msg;
            }
        }
    }
}
