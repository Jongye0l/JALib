package kr.jongyeol.jaServer.data;

import kr.jongyeol.jaServer.Variables;

import java.util.ArrayList;
import java.util.List;

public class AutoRemovedData {
    private static final List<AutoRemovedData> autoRemovedDataList = new ArrayList<>();
    private static boolean running = false;

    private static void checkThread() {
        if(running) return;
        running = true;
        new Thread(AutoRemovedData::autoRemove).start();
    }

    private static void autoRemove() {
        AutoRemovedData autoRemovedData;
        synchronized(autoRemovedDataList) {
            if(autoRemovedDataList.isEmpty()) return;
            autoRemovedData = autoRemovedDataList.get(0);
            for(AutoRemovedData data : autoRemovedDataList)
                if(data.removeTime < autoRemovedData.removeTime) autoRemovedData = data;
        }
        long time = autoRemovedData.removeTime - System.currentTimeMillis() - 10;
        if(time < 0) time = 0;
        AutoRemovedData finalAutoRemovedData = autoRemovedData;
        try {
            Thread.sleep(time);
        } catch (InterruptedException e) {
            e.printStackTrace();
        }
        finalAutoRemovedData.remove();
        autoRemove();
    }

    public long removeTime;

    public AutoRemovedData() {
        synchronized(autoRemovedDataList) {
            autoRemovedDataList.add(this);
            use();
        }
        checkThread();
    }

    public void remove() {
        synchronized(autoRemovedDataList) {
            autoRemovedDataList.remove(this);
        }
        onRemove();
        Variables.setNull(this);
    }

    public void use() {
        removeTime = System.currentTimeMillis() + 600000;
    }

    public void onRemove() {
    }
}