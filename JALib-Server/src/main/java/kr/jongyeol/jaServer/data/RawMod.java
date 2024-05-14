package kr.jongyeol.jaServer.data;

import lombok.AllArgsConstructor;
import lombok.Data;

@Data
@AllArgsConstructor
public class RawMod {
    public ModData mod;
    public Version version;
}
