using System;
using System.Collections;
class ProtocolStr : ProtocolBase    //这个类没有什么用的 只是写出来玩一玩
{
    //传输的字符串
    public string str;
    //解码器
    public override ProtocolBase Decode(byte[] readbuff, int start, int length)
    {
        ProtocolStr protocol = new ProtocolStr();
        protocol.str = System.Text.Encoding.UTF8.GetString(readbuff, start, length);
        return (ProtocolBase)protocol;
    }
    //编码器
    public override byte[] Encode()
    {
        byte[] b = System.Text.Encoding.UTF8.GetBytes(str);
        return b;
    }
    //协议名称
    public override string GetName()
    {
        if (str.Length == 0) return "";
        return str.Split(',')[0];
    }
    //协议描述
    public override string GetDesc()
    {
        return "[ProtocolStr]:" + str;
    }
}