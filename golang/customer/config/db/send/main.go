package main

import (
	"daprdemos/golang/customer/config/db"
	"daprdemos/golang/customer/models"
	"fmt"
	"strconv"
)

func main() {
	fmt.Println("start ...")
	var count int
	db.DB.Model(&models.Customer{}).Count(&count)
	if count == 0 {
		for index := 0; index < 100; index++ {
			db.DB.Create(&models.Customer{
				Name: "小红" + strconv.Itoa(index),
			})
		}
	}
	fmt.Println("done")
}
