package db

import "daprdemos/golang/customer/models"

func init() {
	DB.AutoMigrate(&models.Customer{})
}
