You can set PE (Exe) Version Info (Metadata parameters) from config xml

      <Property name="Comments">$FILE_COMMENT$</Property>       
	  <Property name="CompanyName">$FILE_COMPANY$</Property>       
	  <Property name="FileDescription">$FILE_DESCRIPTION$</Property>       
	  <Property name="FileVersion">$FILE_VERSION$</Property>       
	  <Property name="InternalName"></Property>       
	  <Property name="LegalCopyright">$FILE_COPYRIGHTS$</Property>       
	  <Property name="LegalTrademarks">$PRODUCT_NAME$</Property>       
	  <Property name="OriginalFilename"></Property>       
	  <Property name="ProductName">$PRODUCT_NAME$</Property>       
	  <Property name="ProductVersion">$PRODUCT_VERSION$</Property>       
   
	  <Property name="Assembly Version">$FILE_VERSION$</Property>       	
	  <Property name="PrivateBuild">$FILE_VERSION$</Property>       
	  <Property name="SpecialBuild">$FILE_VERSION$</Property>       

FULL LIST: https://learn.microsoft.com/ru-ru/windows/win32/menurc/versioninfo-resource    

PROPERTIES REPLACEMENTS:
	
	%CD% - Current Directory
	
	%ENVIRONMENT_VARIABLE% - Windows Environment Variables
	%ALLUSERSPROFILE%
	%APPDATA%
	%CommonProgramFiles%
	%COMMONPROGRAMFILES(x86)%
	%COMPUTERNAME%
	%DATE%
	%HOMEDRIVE%
	%HOMEPATH%
	%LOCALAPPDATA%
	%PROCESSOR_ARCHITECTURE%
	%ProgramData%
	%ProgramFiles%
	%ProgramFiles(x86)%
	%ProgramW6432%
	%Public%
	%RANDOM%
	%SYSTEMDRIVE%
	%SYSTEMROOT%
	%TEMP%
	%TIME%
	%TMP%
	%USERNAME%
	%USERPROFILE%
	%WINDIR%
	
	%ANY_NAME% - FOR REPLACE WITH REGEX (?<ANY_NAME>...)
	%FileName% 
	%FileVersion% 
	
	$FILE_COMPANY$
	$FILE_DESCRIPTION$
	$FILE_COMMENT$
	$FILE_COMMENT$
	$FILE_VERSION$
	$FILE_LANGUAGE$
	$FILE_COPYRIGHTS$
	$PRODUCT_NAME$
	$PRODUCT_VERSION$
