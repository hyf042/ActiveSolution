<?php
	require_once('base.php');

if (!isset($_GET['type']))
{
	$_GET['type'] = 6;
}

switch($_GET['type'])
{
case 0:
	?>
	Get Active Code<p/>
	<form name="get_form" id="get_form" method="post" action="index.php?type=3">
		<input name="game" id="game" type="text" size="30" placeholder="Please input your game name" />
		<br/>
		<input name="number" id="number" type="text" size="30" placeholder="The number of codes you want" />
		<br/>
		<button name="submit" type="submit" id="submit_btn">Submit</button>
	</form>
	<?php
	break;
case 1:
	?>
	Check your code<p/>
	<form name="get_form" id="get_form" method="post" action="index.php?type=4">
		<input name="code" id="code" type="text" size="40" placeholder="Please Input your code" />
		<button name="submit" type="submit" id="submit_btn">Submit</button>
	</form>
	<?php
	break;
case 2:
	?>
	Activate Code<p/>
	<form name="get_form" id="get_form" method="post" action="index.php?type=5">
		<input name="code" id="code" type="text" size="40" placeholder="Please Input your Code" />
		<br/>
		<input name="game" id="game" type="text" size="40" placeholder="Please Input your Game Name" />
		<br/>
		<input name="machine" id="machine" type="text" size="40" placeholder="Please Input your Machine Code" />
		<br/>
		<button name="submit" type="submit" id="submit_btn">Submit</button>
	</form>
	<?php
	break;
case 3:
	if (!isset($_POST['game']))
		exit();

	if (!isset($_POST["number"]))
		$_POST['number'] = 1;

	$cnt = (int)$_POST["number"];
	$game = $_POST['game'];
	$codes = array();
	
	for($i = 0; $i < $cnt; $i++)
		$codes[$i] = genCode($game);

	echo (string)$cnt."<br/><br/>";
	echo '<table border="0">';
	for($i = 0; $i < $cnt; $i++)
		echo "<tr><td>$codes[$i]</td></tr>";
	echo "</table>";
	break;
case 4:
	if (!isset($_POST["code"]))
	{
		echo "no such code";
		exit();
	}
	$code = $_POST["code"];
	if (!existCode($code))
		echo "No such code";
	else if(isActived($code))
		echo "Yes, it's actived";
	else
		echo "No, it's not actived";
	break;
case 5:
	if (!isset($_POST["code"]) || !isset($_POST["game"]) || !isset($_POST["machine"]))
		exit();

	echo $_POST["code"]."\t".$_POST["game"]."\t".$_POST["machine"]."<p/>";
	if (activate($_POST["code"], $_POST["game"], $_POST["machine"]))
		echo "Activate Successful!";
	else
		echo "Activate Failed";

	break;
case 6:
	$codes = getAllCodes();
	$cnt = count($codes);
	?>
	The number of all codes is <?php echo $cnt ?> : <p/>
	<table border="1">
		<?php
		foreach  ($codes as $value)
		{
			echo "<tr>";
			echo "<td>".$value['code']."</td>";
			echo "<td>".$value['game']."</td>";
			echo "<td>".$value['machine']."</td>";
			echo "<td>".$value['actived']."</td>";
			echo "</tr>";
		}
		?>
	</table>
	<?php
	break;
case 7:
	if (!isset($_POST["code"]) || !isset($_POST["game"]) || !isset($_POST["machine"]))
	{
		echo "actived:false";
		exit();
	}
	if (checkActived($_POST["code"], $_POST["game"], $_POST["machine"]))
		echo "actived:true";
	else
		echo "actived:false";
	break;
case 8:
	if (!isset($_POST["code"]) || !isset($_POST["game"]))
	{
		echo "inactivate:false";
		exit();
	}
	if (inactivate($_POST["code"], $_POST["game"]))
		echo "inactivate:true";
	else
		echo "inactivate:false";
	break;
};

?>