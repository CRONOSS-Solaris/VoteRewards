
using System.Collections.Generic;

public class VoterResponse
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string Port { get; set; }
    public string Month { get; set; }
    public List<Voter> Voters { get; set; }
}
public class Voter
{
    public string Nickname { get; set; }
    public int Votes { get; set; }
}