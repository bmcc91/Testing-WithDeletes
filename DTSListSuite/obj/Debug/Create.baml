   M S B A M L   `   `   `  ����  DTSListSuite    DTSListSuite.Create   5      \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5        N� Title$  Create DTS List��5      .��5
      � $��
10,0,10,10q�6    � ColumnDefinitions 5   
   �� 5       � RowDefinitions 5   
   �� 5      �� 5      $
��Auto �6   �� 5      �� 5      .+�5   
   �� $��0��6   $��35��6    �� Padding$ 5q�6*   
�������._�5      �� $��Center=�6   $��Center=�6/   
���   �.��6_   Create DTS List5      5      �� 5   
   $��1��6   $
��Wrap=�6!   ��This method will compile a fresh DTS list with data aggregated from the magicweb database. A compiled list will be created, and will have to be manually uploaded to the Google DTS Sheet. Please refer to the README if you are unsure how to upload this sheet.5"      � 5$   
   -   5        $��
compileBar��5$      $��2��6(   $��25��65   �� 5&   
   -   5        $��compileButton��5&      $��3��6&   $	��125��63   $��25��6?   $��Right=�5'      $��Bottom=�6-   .��6^   	Compile5)      5*      5+      