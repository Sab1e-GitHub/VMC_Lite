   M S B A M L   `   `   `  ����C  ?VMC Lite, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null#clr-namespace:VMC_LiteVMC_Lite  5   	       VMC_Lite.ETS2Tools   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   (localclr-namespace:VMC_Lite     5   	     � Title$  
ETS2 Tools��5   	   $	��400��6   $	��400��6)    � 
ResizeMode$ NoResize=�65   .��5	      � .+�5
   
   �� $��0��6   #��� ��6!   $��1q�6n   $��10q�6�   ._�5      � #��� ��6    � RowDefinitions 5      �� 5      $
��auto �6$   �� 5      $
��auto �6$   �� 5      $��* �6$   .+�5      �� $��0��6    �� Orientation$ Vertical=�6*   .+�5      �� $��Key Binding��6    $
��Left=�63   $��5q�6N   $
��Bold"�6Y   �� 5      -   5         ��Name$ RefreshButton��5      $
��Left=�64   $��80��6P   $��20��6[   $��Refresh��6g   $��10q�6y   6�   5      �� 5      $��0��6   $ Vertical=�6+   .+�5      �� $��Right=�6!   $ 
Horizontal=�6=   $��2,2,2,2q�6V   .+�5      �� $��Game Profile��6    �� 5      $	��160��6#   5      �� 5      $��Right=�6"   $ 
Horizontal=�6>   $��2,2,2,2q�6W   .+�5      �� $��Game Controller��6    �� 5      -   5        $��GameControllerComboBox��5   #   $	��160��6A   6M   5      5      �� 5      $��1��6   $��10,10,10,10q�6)   �� 5!      $��2��6   .��5"      �� $ Vertical=�6"   .+�5$      �� $ 
Horizontal=�6%   .+�5%      �� -   5        $��Bind Light Buttons��5%   &   $	��160��6C   $��10,10,10,10q�6O   6d   �� 5&      $��Current:��6(   $��Center=�68   5'      �� 5)      $ 
Horizontal=�6%   .+�5*      �� $��Bind Light Horn ON��6&   $	��160��6C   $��10,10,10,10q�6O   �� 5+      $��Current:��6(   $��Center=�68   5,      �� 5.      $ 
Horizontal=�6%   .+�5/      �� $��Bind Wipers OFF��6&   $	��160��6@   $��10,10,10,10q�6L   �� 50      $��Current:��6(   $��Center=�68   51      �� 53      $ 
Horizontal=�6%   .+�54      �� $��Bind Wipers Level 1��6&   $	��160��6D   $��10,10,10,10q�6P   �� 55      $��Current:��6(   $��Center=�68   56      �� 58      $ 
Horizontal=�6%   .+�59      �� $��Bind Wipers Level 2��6&   $	��160��6D   $��10,10,10,10q�6P   �� 5:      $��Current:��6(   $��Center=�68   5;      �� 5=      $ 
Horizontal=�6%   .+�5>      �� $��Bind Wipers Level 3��6&   $	��160��6D   $��10,10,10,10q�6P   �� 5?      $��Current:��6(   $��Center=�68   5@      �� 5B      $ 
Horizontal=�6%   .+�5C      �� $��Bind Parking Light ON��6&   $	��160��6F   $��10,10,10,10q�6R   �� 5D      $��Current:��6(   $��Center=�68   5E      �� 5G      $ 
Horizontal=�6%   .+�5H      �� $��Bind Low Beam Light ON��6&   $	��160��6G   $��10,10,10,10q�6S   �� 5I      $��Current:��6(   $��Center=�68   5J      �� 5L      $ 
Horizontal=�6%   .+�5M      �� $��Bind High Beam Light ON��6&   $	��160��6H   $��10,10,10,10q�6T   �� 5N      $��Current:��6(   $��Center=�68   5O      �� 5Q      $ 
Horizontal=�6%   .+�5R      �� $!��Bind Left-Turn Indicator ON��6&   $	��160��6L   $��10,10,10,10q�6X   �� 5S      $��Current:��6(   $��Center=�68   5T      �� 5V      $ 
Horizontal=�6%   .+�5W      �� $"��Bind Right-Turn Indicator ON��6&   $	��160��6M   $��10,10,10,10q�6Y   �� 5X      $��Current:��6(   $��Center=�68   5Y      5[      5\      5^      5_      � 5`   
   -   5         �Name$ BindingModeGrid��5`      $y�	Collapsed=�6(   .+�5a      �� -   5         ��Name$ Overlay��5a      
������� J� Opacity$	 0.7Q�5b      $��Stretch=�5d      $��Stretch=�5e      �� 5f      $��Press "Esc" to cancel.��6   $��20(�6@   $��Center=�6N   $��10q�6k   �� 5g      -   5         ��Name$ BindingPromptTextBlock��5g      $��25(�68   $��Center=�6F   $��Center=�6c   $
��Wrap=�6~   5h      5j      5k      