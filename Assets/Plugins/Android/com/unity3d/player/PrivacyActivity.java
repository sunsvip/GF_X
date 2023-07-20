package com.unity3d.player;
import android.app.Activity;
import android.app.AlertDialog;
import android.content.DialogInterface;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.ActivityInfo;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.webkit.WebResourceError;
import android.webkit.WebResourceRequest;
import android.webkit.WebView;
import android.webkit.WebViewClient;

public class PrivacyActivity extends Activity implements DialogInterface.OnClickListener {
    boolean privacyEnable = true;
    boolean useLocalHtml = true;
    String privacyUrl = "https://blog.csdn.net/final5788";
    final String htmlStr = "欢迎使用本游戏，在使用本游戏前，请您充分阅读并理解<a href=\"https://blog.csdn.net/final5788\">《用户协议》</a>和<a href=\"https://www.baidu.com\">《隐私政策》</a>各条\n" +
            "款，了解我们对于个人信息的处理规则和权限申请的目的，特别提醒您注意前述协议中关于\n" +
            "我们免除自身责任，限制您的权力的相关条款及争议解决方式，司法管辖等内容。我们将严\n" +
            "格遵守相关法律法规和隐私政策以保护您的个人隐私。为确保您的游戏体验，我们会向您申请以下必要权限，您可选择同意或者拒绝，拒绝可能会导致无法进入本游戏。同时，我们会根据本游戏中相关功能的具体需要向您申请非必要的权限，您可选择同意或者拒绝，拒绝可能会导致部分游戏体验异常。其中必要权限包括：设备权限(必要)：读取唯一设备标识 (AndroidID、mac)，生成帐号、保存和恢复游戏数据，识别异常状态以及保障网络及运营安全。存储权限(必要)：访问您的存储空间，以便使您可以下载并保存内容、图片存储及上传、个人设置信息缓存读写、系统及日志文件创建。\n";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        ActivityInfo actInfo = null;
        try {
            //获取AndroidManifest.xml配置的元数据
            actInfo = this.getPackageManager().getActivityInfo(getComponentName(), PackageManager.GET_META_DATA);
            privacyEnable = actInfo.metaData.getBoolean("privacyEnable");
            useLocalHtml = actInfo.metaData.getBoolean("useLocalHtml");
            privacyUrl = actInfo.metaData.getString("privacyUrl");
        } catch (PackageManager.NameNotFoundException e) {
            e.printStackTrace();
        }

        //如果已经同意过隐私协议则直接进入Unity Activity
        if (!privacyEnable || GetPrivacyAccept()){
            EnterUnityActivity();
            return;
        }
        ShowPrivacyDialog();//弹出隐私协议对话框
    }

    @Override
    public void onClick(DialogInterface dialogInterface, int i) {
        switch (i){
            case AlertDialog.BUTTON_POSITIVE://点击同意按钮
                SetPrivacyAccept(true);
                EnterUnityActivity();//启动Unity Activity
                break;
            case AlertDialog.BUTTON_NEGATIVE://点击拒绝按钮,直接退出App
                finish();
                break;
        }
    }
    private void ShowPrivacyDialog(){
        WebView webView = new WebView(this);
        if (useLocalHtml){
            webView.loadDataWithBaseURL(null, htmlStr, "text/html", "UTF-8", null);
        }else{
            webView.loadUrl(privacyUrl);
            webView.setWebViewClient(new WebViewClient(){
                @Override
                public boolean shouldOverrideUrlLoading(WebView view, String url) {
                    view.loadUrl(url);
                    return true;
                }

                @Override
                public void onReceivedError(WebView view, WebResourceRequest request, WebResourceError error) {
                    view.reload();
                }

                @Override
                public void onPageFinished(WebView view, String url) {
                    super.onPageFinished(view, url);
                }
            });
        }

        AlertDialog.Builder privacyDialog = new AlertDialog.Builder(this);
        privacyDialog.setCancelable(false);
        privacyDialog.setView(webView);
        privacyDialog.setTitle("User Terms & Privacy");
        privacyDialog.setNegativeButton("Exit",this);
        privacyDialog.setPositiveButton("Agree",this);
        privacyDialog.create().show();
    }
//启动Unity Activity
    private void EnterUnityActivity(){
        Intent unityAct = new Intent();
        unityAct.setClassName(this, "com.unity3d.player.UnityPlayerActivity");
        this.startActivity(unityAct);
    }
//保存同意隐私协议状态
    private void SetPrivacyAccept(boolean accepted){
        SharedPreferences.Editor prefs = this.getSharedPreferences("PlayerPrefs", MODE_PRIVATE).edit();
        prefs.putBoolean("PrivacyAccepted", accepted);
        prefs.apply();
    }
    private boolean GetPrivacyAccept(){
        SharedPreferences prefs = this.getSharedPreferences("PlayerPrefs", MODE_PRIVATE);
        return prefs.getBoolean("PrivacyAccepted", false);
    }
}