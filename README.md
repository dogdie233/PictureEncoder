# PictureEncoder - 一个图片加密器(不过会让图片体积增大)

把一张图片加密成可以显示，但是完全看不出内容的样子

加密方法是乱写的好嘛

学校的任务罢了

## 使用方法

是很标准的控制台工具，使用方法就和其他的控制台工具一样

```cmd
PictureEncoder [命令] [选项]
```

### 命令

```cmd
encode <输入图片路径>  加密图片
decode <输入图片路径>  解密图片
```

### 参数

```cmd
--version       打印版本信息
-?, -h, --help  打印帮助信息
-p <p>          密码
-o <o>          输出的文件名
-debug          Debug模式，会输出奇怪的东西
```

## 致谢(排名不分先后)

- [日志库 NLog](https://github.com/NLog/NLog)
- [图片处理库 ImageSharp](https://github.com/SixLabors/ImageSharp)
- [命令行接口 CommandLineAPI](https://github.com/dotnet/command-line-api)
