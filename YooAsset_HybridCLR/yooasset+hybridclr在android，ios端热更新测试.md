这个工程是用来学习YooAsset和Hybridcl，来做unity资源和代码热更新，
实现了android 和ios 双端，跑通

# yooAsset git网址
https://github.com/tuyoogame/YooAsset
官方文档：https://www.yooasset.com/
# hybridclr git网址
https://github.com/focus-creative-games
官方文档：https://hybridclr.doc.code-philosophy.com

# 效果图
## 苹果手机

![[862fc0a15dc17136f226dc2ce126605.jpg]]
## 安卓手机
![[de39b9a60d1b7a6797b4c26f98510e0.jpg]]

# 注意事项
1.文档一定要详细的看，很多问题在文档中就有解决办法

例如遇到iOS打包时候的报错解决办法就在常见问题中，
### [打包iOS时出现 Undefined symbols： RuntimeApi_LoadMetadataForAOTAssembly 或 hybridclrApi_LoadMetadataForAOTAssembly](https://hybridclr.doc.code-philosophy.com/#/help/commonerrors?id=%e6%89%93%e5%8c%85ios%e6%97%b6%e5%87%ba%e7%8e%b0-undefined-symbols%ef%bc%9a-runtimeapi_loadmetadataforaotassembly-%e6%88%96-hybridclrapi_loadmetadataforaotassembly)

因为你使用的是原始libil2cpp.a。请根据 [build iOS libil2cpp.a](https://hybridclr.doc.code-philosophy.com/#/basic/buildpipeline) 文档编译最新的。然后替换xcode项目中的libil2cpp.a文件

![[38af996cc0985be65c4a964fba002d6.png]]
![[cdce1307b75fc3bf87eb0bbb72116d2.png]]

![[bf0a2ed3e9a8d31b654532664776d03.png]]


# 最后欢迎加入胖墩游戏圈 星球，获取更多资源。![[../Readme图片/胖墩星球号.jpeg]]