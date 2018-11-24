using System;
using System.Collections;
using System.Collections.Generic;
public class Player
{
    string name;
    public Player(string name)
    {
        this.name = name;
    }
    public Player(){}
    public string Name
    {
        get { return name; }
        set { name = value; }
    }
}