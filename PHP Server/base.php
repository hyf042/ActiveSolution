<?php

require_once("activeCode.php");
require_once("config.php");

ob_start();

$linkdb = mysql_connect($dbhost,$dbuser,$dbpwd);

if ($linkdb)
{
	if (!mysql_select_db($dbname,$linkdb))
		exit();
}
else
	exit();

function existCode($code)
{
	$res = mysql_query("SELECT * FROM codes WHERE code = '$code'");
	if ($res) {
		$m = mysql_fetch_array($res, MYSQL_ASSOC);
		if ($m)
			return true;
	}
	return false;
}
function insertCode($code, $game)
{
	if (existCode($code))
		return false;
	return mysql_query("INSERT INTO codes VALUES ('$code', '$game', 'none', 0)");
}
function getCodeInfo($code)
{
	$res = mysql_query("SELECT * FROM codes WHERE code = '$code'");
	if ($res) {
		$m = mysql_fetch_array($res, MYSQL_ASSOC);
		if ($m)
			return $m;
	}
	return false;
}
function deleteCode($code)
{
	if (mysql_query("DELETE FROM codes WHERE code = '$code'"))
		return mysql_affected_rows() == 1;
	return false;
}
function genCode($game)
{
	do {
		$code = ActiveCode::genActiveCode();
	}while(existCode($code));

	if (insertCode($code, $game))
		return $code;
	else
		return false;
}
function activate($code, $game, $machine)
{
	$info = getCodeInfo($code);
	if (!$info || $info['game'] != $game)
		return false;

	if (mysql_query("UPDATE codes SET machine = '$machine', actived = 1 WHERE code = '$code'"))
		return true;
	return false;
}
function inactivate($code, $game)
{
	$info = getCodeInfo($code, $game);
	if (!$info || $info['game'] != $game)
		return false;

	if (mysql_query("UPDATE codes SET machine = 'none', actived = 0 WHERE code = '$code'"))
		if (mysql_affected_rows() == 1)
			return true;
	return false;
}
function isActived($code)
{
	$info = getCodeInfo($code);
	return $info && $info['actived'] == 1;
}
function checkActived($code, $game, $machine)
{
	$info = getCodeInfo($code);
	return $info && $info['actived'] == 1 && $info['game'] == $game && $info['machine'] == $machine;
}
function getAllCodes()
{
	$codes = array();
	$ret = mysql_query("SELECT * FROM codes");
	if ($ret)
	{
		while($row=mysql_fetch_array($ret))
			$codes[]=$row;
	}
	return $codes;
}

?>