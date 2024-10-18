package kr.jongyeol.jaServer.controller;

import jakarta.servlet.http.HttpServletRequest;
import kr.jongyeol.jaServer.data.ForceUpdateHandle;
import kr.jongyeol.jaServer.data.ModData;
import kr.jongyeol.jaServer.data.Version;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;

@Controller
public class ModApplicatorController extends CustomController {
    @GetMapping("/modApplicator/{name}/{version}")
    public String modApplicator(HttpServletRequest request, @PathVariable String name, @PathVariable String version, Model model) {
        info(request, "ModApplicator: " + name + " " + version);
        ModData modData = ModData.getModData(name);
        boolean forceUpdate = true;
        Version ver = new Version(version);
        if(!ver.equals(modData.getVersion()) && !ver.equals(modData.getBetaVersion())) {
            boolean isBeta = modData.getBetaMap().get(ver);
            if(modData.isForceUpdate()) model.addAttribute("updateType", "모드");
            else if(modData.isForceUpdateBeta() && isBeta) model.addAttribute("updateType", "베타");
            else {
                forceUpdate = false;
                for(ForceUpdateHandle handle : modData.getForceUpdateHandles())
                    if(handle.checkVersion(ver)) forceUpdate = handle.forceUpdate;
                if(forceUpdate) model.addAttribute("updateType", "버전");
            }
            if(forceUpdate) model.addAttribute("oldFullName", " " + name + " " + version);
            version = (isBeta ? modData.getBetaVersion() : modData.getVersion()).toString();
        }
        model.addAttribute("name", name);
        model.addAttribute("version", version);
        model.addAttribute("fullName", " " + name + " " + version);
        model.addAttribute("redirect", forceUpdate);
        return "ModApplicator-kr";
    }
}
