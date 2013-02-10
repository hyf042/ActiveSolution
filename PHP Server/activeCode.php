<?php

define('Length', 16);
define('Token_Size', 4);

class ActiveCode
{
	private static function genRandomString($len) 
	{ 
	    $chars = array( 
	        "A", "B", "C", "D", "E", "F", "G",  
	        "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R",  
	        "S", "T", "U", "V", "W", "X", "Y", "Z", "2",  
	        "3", "4", "5", "6", "7", "8", "9" 
	    ); 
	    $charsLen = count($chars) - 1; 
	 
	    shuffle($chars);    // 将数组打乱 
	     
	    $output = ""; 
	    for ($i=0; $i<$len; $i++) 
	    { 
	        $output .= $chars[mt_rand(0, $charsLen)]; 
	    } 
	 
	    return $output; 
	} 
	public static function genActiveCode()
	{
		$code = chunk_split(ActiveCode::genRandomString(Length), Token_Size, '-');
		return trim($code,'-');
	}
}

?>