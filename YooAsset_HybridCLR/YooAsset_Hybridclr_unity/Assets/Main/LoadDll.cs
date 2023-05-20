using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using YooAsset;

public class LoadDll : MonoBehaviour
{
    // 资源系统运行模式
    public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

    //CDN地址
    public string DefaultHostServer = ""; //资源服务器地址
    public string FallbackHostServer = ""; //资源服务器地址

    public string HotDllName = "HotFix.dll";

    //补充元数据dll的列表，Yooasset中不需要带后缀
    public static List<string> AOTMetaAssemblyNames { get; } = new List<string>()
    {
        "mscorlib.dll",
        "System.dll",
        "System.Core.dll",
       
    };

    //获取资源二进制
    private static Dictionary<string, byte[]> s_assetDatas = new Dictionary<string, byte[]>();
    public static byte[] GetAssetData(string dllName)
    {
        return s_assetDatas[dllName];
    }


    void Start()
    {
        DefaultHostServer = GetHostServerURL();
        FallbackHostServer = GetHostServerURL();

        StartCoroutine(DownLoadAssetsByYooAssets(this.StartGame));
    }


    #region Yooasset下载

    IEnumerator DownLoadAssetsByYooAssets(Action onDownloadComplete)
    {
        // 1.初始化资源系统
        YooAssets.Initialize();

        // 创建默认的资源包
        var package = YooAssets.CreatePackage("DefaultPackage");
        // 设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
        YooAssets.SetDefaultPackage(package);

        if (PlayMode == EPlayMode.EditorSimulateMode)
        {
            //编辑器模拟模式
            var initParameters = new EditorSimulateModeParameters();
            initParameters.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
            yield return package.InitializeAsync(initParameters);
        }
        else if (PlayMode == EPlayMode.HostPlayMode)
        {
            //联机运行模式
            var initParameters = new HostPlayModeParameters();
            initParameters.QueryServices = new QueryStreamingAssetsFileServices();
            initParameters.DefaultHostServer = DefaultHostServer;
            initParameters.FallbackHostServer = FallbackHostServer;
            yield return package.InitializeAsync(initParameters);
        }
        else if (PlayMode == EPlayMode.OfflinePlayMode)
        {
            //单机模式
            var initParameters = new OfflinePlayModeParameters();
            yield return package.InitializeAsync(initParameters);
        }
        Debug.Log("资源系统初始化完成。");

        //2.获取资源版本
        var operation = package.UpdatePackageVersionAsync();
        yield return operation;

        if (operation.Status != EOperationStatus.Succeed)
        {
            //更新失败
            Debug.Log("获取资源版本失败！");
            Debug.LogError(operation.Error);
            //TODO
            yield break;
        }
        string PackageVersion = operation.PackageVersion;

        //3.更新补丁清单
        var operation2 = package.UpdatePackageManifestAsync(PackageVersion);
        yield return operation2;

        if (operation2.Status != EOperationStatus.Succeed)
        {
            //更新失败
            Debug.Log("补丁清单更新失败！");
            Debug.LogError(operation2.Error);
            //TODO:
            yield break;
        }

        //4.下载补丁包
        yield return Download();

        //TODO:判断是否下载成功...
        //热更新Dll名称
        var Allassets = new List<string>
        {
            HotDllName,
            
        }.Concat(AOTMetaAssemblyNames);
        
        foreach (var asset in Allassets)
        {
            RawFileOperationHandle handle = package.LoadRawFileAsync(asset);
            //var handle = package.LoadAssetAsync<GameObject>(asset);
            yield return handle;
            byte[] fileData = handle.GetRawFileData();
            s_assetDatas[asset] = fileData;
            Debug.Log($"dll:{asset}  size:{fileData.Length}"+"   ="+handle.GetRawFileText());
        }

        onDownloadComplete();
    }

    IEnumerator Download()
    {
        int downloadingMaxNum = 10;
        int failedTryAgain = 3;
        int timeout = 60;
        var package = YooAssets.GetPackage("DefaultPackage");
        var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain, timeout);

        //没有需要下载的资源
        if (downloader.TotalDownloadCount == 0)
        {
            Debug.Log("没有资源更新");
            yield break;
        }

        //需要下载的文件总数和总大小
        int totalDownloadCount = downloader.TotalDownloadCount;
        long totalDownloadBytes = downloader.TotalDownloadBytes;
        Debug.Log($"文件总数:{totalDownloadCount}:::总大小:{totalDownloadBytes}");
        //注册回调方法
        downloader.OnDownloadErrorCallback = OnDownloadErrorFunction;
        downloader.OnDownloadProgressCallback = OnDownloadProgressUpdateFunction;
        downloader.OnDownloadOverCallback = OnDownloadOverFunction;
        downloader.OnStartDownloadFileCallback = OnStartDownloadFileFunction;

        //开启下载
        downloader.BeginDownload();
        yield return downloader;

        //检测下载结果
        if (downloader.Status == EOperationStatus.Succeed)
        {
            //下载成功
            Debug.Log("更新完成!");
            //TODO:
        }
        else
        {
            //下载失败
            Debug.LogError("更新失败！");
            //TODO:
        }
    }

    /// <summary>
    /// 开始下载
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="sizeBytes"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnStartDownloadFileFunction(string fileName, long sizeBytes)
    {
        Debug.Log(string.Format("开始下载：文件名：{0}, 文件大小：{1}", fileName, sizeBytes));
    }

    /// <summary>
    /// 下载完成
    /// </summary>
    /// <param name="isSucceed"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnDownloadOverFunction(bool isSucceed)
    {
        Debug.Log("下载" + (isSucceed ? "成功" : "失败"));
    }

    /// <summary>
    /// 更新中
    /// </summary>
    /// <param name="totalDownloadCount"></param>
    /// <param name="currentDownloadCount"></param>
    /// <param name="totalDownloadBytes"></param>
    /// <param name="currentDownloadBytes"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnDownloadProgressUpdateFunction(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
    {
        Debug.Log(string.Format("文件总数：{0}, 已下载文件数：{1}, 下载总大小：{2}, 已下载大小：{3}", totalDownloadCount, currentDownloadCount, totalDownloadBytes, currentDownloadBytes));
    }

    /// <summary>
    /// 下载出错
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="error"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnDownloadErrorFunction(string fileName, string error)
    {
        Debug.LogError(string.Format("下载出错：文件名：{0}, 错误信息：{1}", fileName, error));
    }

    // 内置文件查询服务类
    private class QueryStreamingAssetsFileServices : IQueryServices
    {
        public bool QueryStreamingAssets(string fileName)
        {
            // 注意：使用了BetterStreamingAssets插件，使用前需要初始化该插件！
            string buildinFolderName = YooAssets.GetStreamingAssetBuildinFolderName();
            return StreamingAssetsHelper.FileExists($"{buildinFolderName}/{fileName}");
        }
    }

    #endregion
    private static Assembly _hotUpdateAss;
    void StartGame()
    {
        LoadMetadataForAOTAssemblies();

#if UNITY_ANDROID || UNITY_IPHONE
        byte[] hooles = GetAssetData("HotUpdate.dll");
        _hotUpdateAss =  System.Reflection.Assembly.Load(hooles);
#endif
        //委托加载方式，加载prefab
        var package = YooAssets.GetPackage("DefaultPackage");
        AssetOperationHandle handle = package.LoadAssetAsync<GameObject>("HotUpdatePrefab");
        handle.Completed += Handle_Completed;
        
        Type entryType = _hotUpdateAss.GetType("Entry");
        entryType.GetMethod("Start").Invoke(null, null);
    }
    private void Handle_Completed(AssetOperationHandle obj)
    {
        GameObject go = obj.InstantiateSync();
        Debug.Log($"Prefab name is {go.name}");
    }


    /// <summary>
    /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
    /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
    /// </summary>
    private static void LoadMetadataForAOTAssemblies()
    {
        /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
        /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
        HomologousImageMode mode = HomologousImageMode.SuperSet;
        foreach (var aotDllName in AOTMetaAssemblyNames)
        {
            byte[] dllBytes = GetAssetData(aotDllName);
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
            Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
        }
    }
    
    
    /// <summary>
    /// 获取资源服务器地址
    /// </summary>
    private string GetHostServerURL()
    {
        //string hostServerIP = "http://127.0.0.1"; //安卓模拟器地址
        string hostServerIP = "http://127.0.0.1";// 自己服务的地址
        string gameVersion = "package";

#if UNITY_EDITOR
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
            return $"{hostServerIP}/CDN/Android/{gameVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
            return $"{hostServerIP}/CDN/IPhone/{gameVersion}";
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
            return $"{hostServerIP}/CDN/WebGL/{gameVersion}";
        else
            return $"{hostServerIP}/CDN/PC/{gameVersion}";
#else
		if (Application.platform == RuntimePlatform.Android)
			return $"{hostServerIP}/CDN/Android/{gameVersion}";
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
			return $"{hostServerIP}/CDN/IPhone/{gameVersion}";
		else if (Application.platform == RuntimePlatform.WebGLPlayer)
			return $"{hostServerIP}/CDN/WebGL/{gameVersion}";
		else
			return $"{hostServerIP}/CDN/PC/{gameVersion}";
#endif
    }
}
