package main

import (
	"daprdemos/golang/customer/config/db"
	"daprdemos/golang/customer/models"
	"fmt"
	"strconv"

	"github.com/google/uuid"
)

func main() {
	fmt.Println("start ...")
	var count int
	db.DB.Model(&models.Customer{}).Count(&count)
	if count == 0 {
		for index := 0; index < 100; index++ {
			var guid uuid.UUID
			if index == 0 {
				guid, _ = uuid.Parse("1e88e584-dcbd-44f6-9960-53c2ad687399")
			}
			db.DB.Create(&models.Customer{
				ID:   guid,
				Name: "小红" + strconv.Itoa(index),
			})
		}
	}
	fmt.Println("done")
}
