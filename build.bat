@echo off
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /codepage:65001 /optimize+ /win32icon:icon.ico "/out:AutoClicker by BOZYILDIZ.exe" AutoClicker.cs
